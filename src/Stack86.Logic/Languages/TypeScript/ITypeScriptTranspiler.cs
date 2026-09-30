namespace Stack86.Logic.Languages.TypeScript;

/// <summary>
/// Transpiles TypeScript source code to JavaScript.
/// </summary>
public interface ITypeScriptTranspiler
{
    /// <summary>
    /// Transpiles the given TypeScript source to JavaScript.
    /// </summary>
    /// <param name="typeScriptSource">The TypeScript source code.</param>
    /// <param name="cancellationToken">Token used to cancel the child-process transpile.</param>
    /// <returns>The transpiled JavaScript source code.</returns>
    /// <exception cref="Stack86.Common.Exceptions.ExternalToolException">Thrown when the transpilation fails.</exception>
    Task<string> TranspileAsync(string typeScriptSource, CancellationToken cancellationToken = default);
}
