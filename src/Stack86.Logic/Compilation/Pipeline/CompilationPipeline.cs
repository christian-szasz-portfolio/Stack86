namespace Stack86.Logic.Compilation.Pipeline;

using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Compilation.Stages;
using Stack86.Logic.Compilation.Stages.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Pipeline.CodeGen;
using Stack86.Logic.Pipeline.Ir;
using Stack86.Logic.Resilience;

/// <summary>
/// Fluent pipeline builder: chained stage calls compose a sequence that is then executed
/// either as a single result (<see cref="RunAsync"/>) or as a stream of progress events
/// (<see cref="RunStreamingAsync"/>). Each stage invokes the relevant language service
/// resolved from the <see cref="ILanguageRegistry"/>.
/// </summary>
public sealed class CompilationPipeline
{
    /// <summary>
    /// Minimum pause between streamed events so each TCP write is flushed separately and
    /// produces a visible real-time effect in the client console.
    /// </summary>
    private const int StageDelayMs = 150;

    /// <summary>Capacity of the bounded producer/consumer channel for streaming events.</summary>
    private const int StreamChannelCapacity = 64;

    /// <summary>Idle interval after which a heartbeat event is emitted.</summary>
    private const int DefaultHeartbeatIntervalMs = 10_000;

    private static readonly Asm8086Generator Generator = new();

    private readonly ILanguageRegistry registry;
    private readonly IIrValidator irValidator;
    private readonly CompilationContext context;
    private readonly List<PipelineStage> stages = [];
    private IResiliencePipelineFactory resilience = NoOpResiliencePipelineFactory.Instance;
    private int heartbeatIntervalMs = DefaultHeartbeatIntervalMs;
    private string? failureMessage;

    private CompilationPipeline(
        ILanguageRegistry registry,
        IIrValidator irValidator,
        SupportedLanguage language,
        IReadOnlyDictionary<string, string> files)
    {
        this.registry = registry;
        this.irValidator = irValidator;
        this.context = new CompilationContext
        {
            Language = language,
            Files = files,
            CurrentLanguage = language,
        };
    }

    /// <summary>Creates a new pipeline targeting the specified language and source files.</summary>
    public static CompilationPipeline For(
        ILanguageRegistry registry,
        IIrValidator irValidator,
        SupportedLanguage language,
        IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(irValidator);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(files);
        return new CompilationPipeline(registry, irValidator, language, files);
    }

    /// <summary>Configures the pipeline using the recommended stage chain for the source language.</summary>
    public CompilationPipeline UseDefaultsFor(SupportedLanguage language)
    {
        LanguageDefaults.ConfigureFor(this, language);
        return this;
    }

    /// <summary>
    /// Configures the pipeline with a resilience factory. When omitted, no timeouts,
    /// retries or circuit breakers are applied.
    /// </summary>
    public CompilationPipeline WithResilience(IResiliencePipelineFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.resilience = factory;
        return this;
    }

    /// <summary>
    /// Overrides the idle interval after which heartbeat events are written to the stream.
    /// Primarily useful for tests; production should rely on the default.
    /// </summary>
    public CompilationPipeline WithHeartbeatInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        this.heartbeatIntervalMs = (int)interval.TotalMilliseconds;
        return this;
    }

    /// <summary>Resolves per-language source preprocessing (currently <c>#include</c> for C).</summary>
    public CompilationPipeline Preprocess()
    {
        this.stages.Add(new PipelineStage("Preprocess", this.RunPreprocessAsync));
        return this;
    }

    /// <summary>Runs an external syntax checker (TCC for C, Roslyn for C#).</summary>
    public CompilationPipeline ValidateExternal()
    {
        this.stages.Add(new PipelineStage("ValidateExternal", this.RunValidateExternalAsync));
        return this;
    }

    /// <summary>Validates that the source uses only features supported by the 8086 target.</summary>
    public CompilationPipeline ValidateCapabilities()
    {
        this.stages.Add(new PipelineStage("ValidateCapabilities", this.RunValidateCapabilitiesAsync));
        return this;
    }

    /// <summary>Transpiles to a more primitive language (C++→C, TypeScript→JavaScript).</summary>
    public CompilationPipeline Transpile()
    {
        this.stages.Add(new PipelineStage("Transpile", this.RunTranspileAsync));
        return this;
    }

    /// <summary>Lexes, parses, and lowers source to IR.</summary>
    public CompilationPipeline LowerToIr()
    {
        this.stages.Add(new PipelineStage("LowerToIr", this.RunLowerToIrAsync));
        return this;
    }

    /// <summary>Runs IR-level optimisations.</summary>
    public CompilationPipeline OptimizeIr()
    {
        this.stages.Add(new PipelineStage("OptimizeIr", this.RunOptimizeIrAsync));
        return this;
    }

    /// <summary>Validates the IR against the target capability profile.</summary>
    public CompilationPipeline ValidateIr()
    {
        this.stages.Add(new PipelineStage("ValidateIr", this.RunValidateIrAsync));
        return this;
    }

    /// <summary>Lowers the optimised IR to 8086 assembly text.</summary>
    public CompilationPipeline EmitAssembly()
    {
        this.stages.Add(new PipelineStage("EmitAssembly", this.RunEmitAssemblyAsync));
        return this;
    }

    /// <summary>
    /// Runs the configured stages and returns a single aggregated <see cref="CompilationResultDto"/>.
    /// </summary>
    /// <exception cref="CompilationFailedException">Thrown when a stage hard-fails or the compile is rejected.</exception>
    public async Task<CompilationResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.resilience.CompileOverall.ExecuteAsync(
                async ct => await this.RunCoreAsync(ct),
                cancellationToken);
        }
        catch (TimeoutRejectedException)
        {
            throw new CompilationFailedException("Compilation timed out.");
        }
        catch (RateLimiterRejectedException)
        {
            throw new CompilationFailedException("Server is busy. Please retry shortly.");
        }
    }

    /// <summary>
    /// Runs the configured stages, yielding a <see cref="CompileStreamEvent.Log"/> per stage and a final
    /// <see cref="CompileStreamEvent.Complete"/> event with the aggregated result. A bounded
    /// <see cref="System.Threading.Channels.Channel{T}"/> decouples the producer from the consumer
    /// so a slow client exerts natural backpressure on the pipeline. While the producer is busy
    /// without emitting, periodic <see cref="CompileStreamEvent.Heartbeat"/> events keep the
    /// connection alive.
    /// </summary>
    public async IAsyncEnumerable<CompileStreamEvent> RunStreamingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = System.Threading.Channels.Channel.CreateBounded<CompileStreamEvent>(
            new System.Threading.Channels.BoundedChannelOptions(StreamChannelCapacity)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });

        using var producerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var producer = Task.Run(() => this.ProduceStreamEventsAsync(channel.Writer, producerCts.Token), producerCts.Token);

        try
        {
            while (true)
            {
                var readyTask = channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var winner = await Task.WhenAny(readyTask, Task.Delay(this.heartbeatIntervalMs, cancellationToken));

                if (winner != readyTask)
                {
                    yield return new CompileStreamEvent.Heartbeat();
                    continue;
                }

                bool hasMore;
                try
                {
                    hasMore = await readyTask;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (!hasMore)
                {
                    break;
                }

                while (channel.Reader.TryRead(out var evt))
                {
                    yield return evt;
                }
            }
        }
        finally
        {
            producerCts.Cancel();
            try
            {
                await producer.ConfigureAwait(false);
            }
            catch
            {
                // Producer cancellation/exception already surfaced via the channel completion.
            }
        }
    }

    // --- Stage runners ----------------------------------------------------
    private async Task<StageOutcome> RunPreprocessAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.CurrentLanguage == SupportedLanguage.C && ctx.Files.Count > 1)
        {
            ctx.ConsoleMessages.Add("[Info] Resolving #include directives...");
        }

        var preprocessor = this.registry.Get<ILanguagePreprocessor>(ctx.CurrentLanguage);
        PreprocessedSource pp;
        try
        {
            pp = preprocessor is not null
                ? preprocessor.Preprocess(ctx.Files)
                : new PreprocessedSource(string.Join("\n", ctx.Files.Values), [], new HashSet<string>());
        }
        catch (CompilationFailedException ex)
        {
            return this.Fail(ctx, $"[Error] {ex.Message}", ex.Message);
        }

        ctx.Source = pp.Source;
        ctx.LineMappings = pp.LineMappings;
        ctx.SystemHeaders = pp.SystemHeaders;
        return StageOutcome.Continue;
    }

    private async Task<StageOutcome> RunValidateExternalAsync(CompilationContext ctx, CancellationToken ct)
    {
        IReadOnlyList<IrDiagnostic> diags;
        try
        {
            diags = await this.resilience.ExternalValidator.ExecuteAsync(
                async innerCt =>
                {
                    var validator = this.registry.Get<IExternalCodeValidator>(ctx.CurrentLanguage);
                    if (validator is null)
                    {
                        return (IReadOnlyList<IrDiagnostic>)[];
                    }

                    var result = await validator.ValidateAsync(ctx.Source, innerCt);
                    return LibraryDiagnosticFilter.FilterKnownLibraryWarnings(result, ctx.SystemHeaders);
                },
                ct);
        }
        catch (BrokenCircuitException)
        {
            ctx.ConsoleMessages.Add("[Warn] External validator circuit open \u2014 skipping syntax check.");
            ctx.Diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Message = "External validator unavailable (circuit open). Validation skipped.",
                Line = 0,
                Column = 0,
            });
            return StageOutcome.Continue;
        }
        catch (TimeoutRejectedException)
        {
            ctx.ConsoleMessages.Add("[Warn] External validator timed out \u2014 skipping syntax check.");
            ctx.Diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Message = "External validator timed out. Validation skipped.",
                Line = 0,
                Column = 0,
            });
            return StageOutcome.Continue;
        }

        diags = RemapDiagnostics(diags, ctx.LineMappings);

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            ctx.Diagnostics.AddRange(diags);
            foreach (var e in errors)
            {
                ctx.ConsoleMessages.Add(
                    e.Line > 0 ? $"[Error] Line {e.Line}: {e.Message}" : $"[Error] {e.Message}");
            }

            return this.Fail(ctx, "[Error] External validation failed", "External validation failed");
        }

        var warnings = diags.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
        if (ctx.CurrentLanguage == SupportedLanguage.C)
        {
            if (warnings.Any(w => w.Message.Contains("TCC", StringComparison.Ordinal)))
            {
                foreach (var w in warnings.Where(w => w.Message.Contains("TCC", StringComparison.Ordinal)))
                {
                    ctx.ConsoleMessages.Add($"[Warn] {w.Message}");
                }
            }
            else
            {
                ctx.ConsoleMessages.Add("[Info] TCC validation passed");
            }
        }

        ctx.Diagnostics.AddRange(diags);
        return StageOutcome.Continue;
    }

    private Task<StageOutcome> RunValidateCapabilitiesAsync(CompilationContext ctx, CancellationToken ct)
    {
        var capabilityValidator = this.registry.Get<ILanguageCapabilityValidator>(ctx.CurrentLanguage);
        IReadOnlyList<IrDiagnostic> diags = capabilityValidator is null ? [] : capabilityValidator.Validate(ctx.Source);
        diags = RemapDiagnostics(diags, ctx.LineMappings);

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            ctx.ConsoleMessages.Add("[Error] Source uses features not supported by the 8086 target");
            foreach (var e in errors)
            {
                ctx.ConsoleMessages.Add($"[Error] Line {e.Line}: {e.Message}");
            }

            ctx.Diagnostics.AddRange(diags);
            return Task.FromResult(StageOutcome.Stop);
        }

        ctx.Diagnostics.AddRange(diags);
        return Task.FromResult(StageOutcome.Continue);
    }

    private async Task<StageOutcome> RunTranspileAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.CurrentLanguage == SupportedLanguage.Cpp)
        {
            ctx.ConsoleMessages.Add("[Info] Transpiling C++ to C...");
        }
        else if (ctx.CurrentLanguage == SupportedLanguage.TypeScript)
        {
            ctx.ConsoleMessages.Add("[Info] Transpiling TypeScript to JavaScript...");
        }

        TranspiledSource transpiled;
        try
        {
            transpiled = await this.resilience.ExternalTranspiler.ExecuteAsync(
                async innerCt =>
                {
                    var transpiler = this.registry.Get<ILanguageTranspiler>(ctx.CurrentLanguage);
                    if (transpiler is null)
                    {
                        return new TranspiledSource(ctx.CurrentLanguage, ctx.Source, []);
                    }

                    var input = new LanguageTranspilerInput(ctx.Source, ctx.Files);
                    return await transpiler.TranspileAsync(input, innerCt);
                },
                ct);
        }
        catch (BrokenCircuitException)
        {
            return this.Fail(
                ctx,
                "[Error] Transpiler unavailable (circuit open). Try again shortly.",
                "Transpiler unavailable (circuit open).");
        }
        catch (TimeoutRejectedException)
        {
            return this.Fail(
                ctx,
                "[Error] Transpiler timed out.",
                "Transpiler timed out.");
        }
        catch (CompilationFailedException ex)
        {
            return this.Fail(ctx, $"[Error] {ex.Message}", ex.Message);
        }

        ctx.Source = transpiled.Source;
        ctx.CurrentLanguage = transpiled.TargetLanguage;
        if (transpiled.LineMappings.Count > 0)
        {
            ctx.LineMappings = transpiled.LineMappings;
        }

        return StageOutcome.Continue;
    }

    private Task<StageOutcome> RunLowerToIrAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.CurrentLanguage == SupportedLanguage.CSharp)
        {
            ctx.ConsoleMessages.Add("[Info] Compiling C# via Roslyn → IR...");
        }
        else
        {
            ctx.ConsoleMessages.Add("[Info] Parsing source code...");
        }

        var frontend = this.registry.Get<ILanguageFrontend>(ctx.CurrentLanguage);
        if (frontend is null)
        {
            var message = $"Unsupported language for IR lowering: '{ctx.CurrentLanguage.Value}'.";
            return Task.FromResult(this.Fail(ctx, $"[Error] {message}", message));
        }

        IrProgram ir;
        try
        {
            var input = new LanguageFrontendInput(ctx.Source, ctx.Files, ctx.SystemHeaders);
            ir = frontend.Lower(input);
        }
        catch (CompilationFailedException ex)
        {
            return Task.FromResult(this.Fail(ctx, $"[Error] {ex.Message}", ex.Message));
        }
        catch (Exception ex) when (ex is UnsupportedSyntaxException
            or CompilerInvariantException
            or UnknownTypeException
            or IndexOutOfRangeException
            or ArgumentOutOfRangeException)
        {
            return Task.FromResult(this.Fail(ctx, $"[Error] {ex.Message}", ex.Message));
        }

        ir = ir with { Diagnostics = RemapDiagnostics(ir.Diagnostics, ctx.LineMappings) };

        if (ctx.Diagnostics.Count > 0)
        {
            ir = ir with { Diagnostics = [.. ctx.Diagnostics, .. ir.Diagnostics] };
            ctx.Diagnostics.Clear();
        }

        ctx.Ir = ir;

        var hasErrors = ir.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        ctx.ConsoleMessages.Add(
            $"[Info] Frontend produced {ir.Functions.Count} function(s), {ir.Globals.Count} global(s)");

        return Task.FromResult(hasErrors ? StageOutcome.Stop : StageOutcome.Continue);
    }

    private Task<StageOutcome> RunOptimizeIrAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.Ir is null)
        {
            return Task.FromResult(StageOutcome.Continue);
        }

        ctx.Ir = IrOptimizer.Optimize(ctx.Ir);
        ctx.ConsoleMessages.Add("[Info] IR optimised");
        return Task.FromResult(StageOutcome.Continue);
    }

    private Task<StageOutcome> RunValidateIrAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.Ir is null)
        {
            return Task.FromResult(StageOutcome.Continue);
        }

        var diags = this.irValidator.Validate(ctx.Ir);
        if (diags.Count > 0)
        {
            ctx.Ir = ctx.Ir with { Diagnostics = [.. ctx.Ir.Diagnostics, .. diags] };
            foreach (var d in diags.Where(d => d.Severity == DiagnosticSeverity.Warning))
            {
                ctx.ConsoleMessages.Add($"[Warn] {d.Message}");
            }
        }

        return Task.FromResult(StageOutcome.Continue);
    }

    private Task<StageOutcome> RunEmitAssemblyAsync(CompilationContext ctx, CancellationToken ct)
    {
        if (ctx.Ir is null)
        {
            return Task.FromResult(StageOutcome.Continue);
        }

        string assembly;
        try
        {
            assembly = Generator.Generate(ctx.Ir);
        }
        catch (CompilationFailedException ex)
        {
            return Task.FromResult(this.Fail(ctx, $"[Error] {ex.Message}", ex.Message));
        }

        ctx.Assembly = assembly;
        var lineCount = assembly.Split('\n').Length;
        ctx.ConsoleMessages.Add($"[Info] Generated {lineCount} lines of 8086 assembly");
        ctx.ConsoleMessages.Add("Build successful");
        return Task.FromResult(StageOutcome.Continue);
    }

    // --- Helpers ----------------------------------------------------------
    private async Task ProduceStreamEventsAsync(
        System.Threading.Channels.ChannelWriter<CompileStreamEvent> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            var totalLines = this.context.Files.Values.Sum(s => s.Split('\n').Length);
            var openingMessage =
                $"[Info] Compiling {this.context.Files.Count} file(s), {totalLines} lines of {this.context.Language.Value.ToUpperInvariant()}...";
            this.context.ConsoleMessages.Add(openingMessage);
            await writer.WriteAsync(new CompileStreamEvent.Log(openingMessage), cancellationToken);
            await Task.Delay(StageDelayMs, cancellationToken);

            try
            {
                await this.resilience.CompileOverall.ExecuteAsync(
                    async ct => await this.ProduceStagesAsync(writer, ct),
                    cancellationToken);
            }
            catch (TimeoutRejectedException)
            {
                await writer.WriteAsync(
                    new CompileStreamEvent.Complete(new CompilationResultDto
                    {
                        Errors = [new DiagnosticDto { Message = "Compilation timed out.", Line = 0, Column = 0 }],
                        ConsoleMessages = [.. this.context.ConsoleMessages],
                    }),
                    cancellationToken);
            }
            catch (RateLimiterRejectedException)
            {
                await writer.WriteAsync(
                    new CompileStreamEvent.Complete(new CompilationResultDto
                    {
                        Errors = [new DiagnosticDto { Message = "Server is busy. Please retry shortly.", Line = 0, Column = 0 }],
                        ConsoleMessages = [.. this.context.ConsoleMessages],
                    }),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Consumer cancelled — drop remaining events.
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task ProduceStagesAsync(
        System.Threading.Channels.ChannelWriter<CompileStreamEvent> writer,
        CancellationToken cancellationToken)
    {
        foreach (var stage in this.stages)
        {
            var snapshot = this.context.ConsoleMessages.Count;
            var outcome = await this.RunStageAsync(stage, cancellationToken);

            for (var i = snapshot; i < this.context.ConsoleMessages.Count; i++)
            {
                await writer.WriteAsync(new CompileStreamEvent.Log(this.context.ConsoleMessages[i]), cancellationToken);
            }

            await Task.Delay(StageDelayMs, cancellationToken);

            if (outcome == StageOutcome.Fail)
            {
                await writer.WriteAsync(
                    new CompileStreamEvent.Complete(new CompilationResultDto
                    {
                        Errors = [new DiagnosticDto { Message = this.failureMessage ?? $"Stage '{stage.Name}' failed.", Line = 0, Column = 0 }],
                        ConsoleMessages = [.. this.context.ConsoleMessages],
                    }),
                    cancellationToken);
                return;
            }

            if (outcome == StageOutcome.Stop)
            {
                break;
            }
        }

        await writer.WriteAsync(new CompileStreamEvent.Complete(this.BuildResultDto()), cancellationToken);
    }

    private async ValueTask<CompilationResultDto> RunCoreAsync(CancellationToken cancellationToken)
    {
        var totalLines = this.context.Files.Values.Sum(s => s.Split('\n').Length);
        this.context.ConsoleMessages.Add(
            $"[Info] Compiling {this.context.Files.Count} file(s), {totalLines} lines of {this.context.Language.Value.ToUpperInvariant()}...");

        foreach (var stage in this.stages)
        {
            var outcome = await this.RunStageAsync(stage, cancellationToken);
            if (outcome == StageOutcome.Fail)
            {
                throw new CompilationFailedException(this.failureMessage ?? $"Stage '{stage.Name}' failed.");
            }

            if (outcome == StageOutcome.Stop)
            {
                break;
            }
        }

        return this.BuildResultDto();
    }

    private async ValueTask<StageOutcome> RunStageAsync(PipelineStage stage, CancellationToken cancellationToken)
    {
        try
        {
            return await this.resilience.DispatchStage.ExecuteAsync(
                async ct => await stage.Run(this.context, ct),
                cancellationToken);
        }
        catch (TimeoutRejectedException)
        {
            return this.Fail(this.context, $"[Error] Stage '{stage.Name}' timed out.", $"Stage '{stage.Name}' timed out.");
        }
    }

    private StageOutcome Fail(CompilationContext ctx, string consoleMessage, string error)
    {
        ctx.ConsoleMessages.Add(consoleMessage);
        this.failureMessage = error;
        return StageOutcome.Fail;
    }

    private CompilationResultDto BuildResultDto()
    {
        var diagnostics = this.context.Ir?.Diagnostics ?? this.context.Diagnostics;

        return new CompilationResultDto
        {
            Assembly = this.context.Assembly,
            Errors = [.. diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => new DiagnosticDto { Message = d.Message, Line = d.Line, Column = d.Column, SourceFile = d.SourceFile })],
            Warnings = [.. diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Warning)
                .Select(d => new DiagnosticDto { Message = d.Message, Line = d.Line, Column = d.Column, SourceFile = d.SourceFile })],
            ConsoleMessages = [.. this.context.ConsoleMessages],
        };
    }

#pragma warning disable SA1204 // Static members should appear before non-static members
    private static IReadOnlyList<IrDiagnostic> RemapDiagnostics(
        IReadOnlyList<IrDiagnostic> diagnostics,
        IReadOnlyList<SourceLineMapping> lineMappings)
    {
        if (lineMappings.Count == 0)
        {
            return diagnostics;
        }

        return [.. diagnostics.Select(d =>
        {
            // line mappings index 0-based by output line - 1
            var idx = d.Line - 1;
            if (idx < 0 || idx >= lineMappings.Count)
            {
                return d;
            }

            var mapping = lineMappings[idx];
            return d with
            {
                Line = mapping.OriginalLine,
                SourceFile = mapping.File,
            };
        })];
    }
#pragma warning restore SA1204 // Static members should appear before non-static members
}
