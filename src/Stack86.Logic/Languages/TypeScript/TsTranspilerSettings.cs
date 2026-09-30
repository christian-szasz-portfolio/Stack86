namespace Stack86.Logic.Languages.TypeScript;

/// <summary>
/// Configuration for the TypeScript-to-JavaScript transpilation step via Node.js.
/// </summary>
public sealed class TsTranspilerSettings
{
    /// <summary>
    /// Path to the Node.js executable. Defaults to <c>node</c> (assumes PATH).
    /// </summary>
    public string NodePath { get; set; } = "node";

    /// <summary>
    /// Path to the <c>transpile.cjs</c> script. When not rooted, resolved relative
    /// to the application base directory.
    /// </summary>
    public string ScriptPath { get; set; } = "transpile.cjs";

    /// <summary>
    /// Path to the <c>node_modules</c> directory containing the <c>typescript</c> package.
    /// When not rooted, resolved relative to the application base directory.
    /// Set as <c>NODE_PATH</c> when invoking the transpiler script.
    /// </summary>
    public string NodeModulesPath { get; set; } = "node_modules";

    /// <summary>
    /// Maximum time in milliseconds to wait for the transpiler to finish. Defaults to 5 000 ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Maximum source code size in bytes. Defaults to 65 536 (64 KB).
    /// </summary>
    public int MaxSourceSizeBytes { get; set; } = 65536;
}
