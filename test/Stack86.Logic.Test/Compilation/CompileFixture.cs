namespace Stack86.Logic.Test.Compilation;

using Microsoft.Extensions.DependencyInjection;
using Stack86.Logic.Compilation;
using Stack86.Logic.Compilation.Providers;
using Stack86.Logic.Languages;
using Stack86.Logic.Languages.TypeScript;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Wires up the real Stack86.Logic compilation pipeline for end-to-end coverage tests.
/// External dependencies (TCC, Node-based TS transpiler) are stubbed so the suite has
/// no shell-out requirements.
/// </summary>
internal static class CompileFixture
{
    public static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // External validator stub claiming the C language.
        var noopExternal = new NoopExternalCodeValidator();
        services.AddSingleton<IExternalCodeValidator>(noopExternal);
        services.AddSingleton<ILanguageService>(noopExternal);

        services.AddSingleton<ITypeScriptTranspiler, IdentityTsTranspiler>();

        services.AddStack86CompilationPipeline();

        return services.BuildServiceProvider();
    }

    public static CompilerProvider BuildProvider() =>
        BuildServices().GetRequiredService<CompilerProvider>();

    public static Task<CompileResult> CompileC(string source) =>
        CompileFiles(SupportedLanguage.C, new Dictionary<string, string> { ["main.c"] = source });

    public static Task<CompileResult> CompileLanguage(SupportedLanguage lang, string source, string fileName) =>
        CompileFiles(lang, new Dictionary<string, string> { [fileName] = source });

    public static Task<CompileResult> CompileFiles(SupportedLanguage lang, IReadOnlyDictionary<string, string> files)
    {
        var provider = BuildProvider();
        var copy = new Dictionary<string, string>(files);
        return CompileResult.RunAsync(() => provider.CompileAsync(lang.Value, copy, CancellationToken.None));
    }

    private sealed class NoopExternalCodeValidator : IExternalCodeValidator
    {
        public SupportedLanguage Language => SupportedLanguage.C;

        public Task<IReadOnlyList<IrDiagnostic>> ValidateAsync(string source, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IrDiagnostic>>([]);
    }

    private sealed class IdentityTsTranspiler : ITypeScriptTranspiler
    {
        public Task<string> TranspileAsync(string source, CancellationToken cancellationToken = default) =>
            Task.FromResult(source);
    }
}
