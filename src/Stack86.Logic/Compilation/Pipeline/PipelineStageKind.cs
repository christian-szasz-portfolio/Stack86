namespace Stack86.Logic.Compilation.Pipeline;

/// <summary>
/// The fixed set of pipeline stages a <see cref="LanguageSpec"/> can declare. Each value
/// maps to a method on <see cref="CompilationPipeline"/> that adds the corresponding stage.
/// </summary>
public enum PipelineStageKind
{
    /// <summary>Per-language source preprocessing (e.g. C <c>#include</c> resolution).</summary>
    Preprocess,

    /// <summary>External syntax check (e.g. TCC for C).</summary>
    ValidateExternal,

    /// <summary>Capability validation against the target's keyword/feature constraints.</summary>
    ValidateCapabilities,

    /// <summary>Transpile to a more primitive language (C++ → C, TypeScript → JavaScript).</summary>
    Transpile,

    /// <summary>Lex, parse, and lower to IR.</summary>
    LowerToIr,

    /// <summary>IR-level optimisations.</summary>
    OptimizeIr,

    /// <summary>IR-level capability validation against the target profile.</summary>
    ValidateIr,

    /// <summary>Emit final 8086 assembly text.</summary>
    EmitAssembly,
}
