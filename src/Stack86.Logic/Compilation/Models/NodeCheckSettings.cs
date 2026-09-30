namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Configuration for JavaScript external validation via <c>node --check</c>.
/// </summary>
public sealed class NodeCheckSettings : ExternalToolSettings
{
    /// <summary>Initializes a new instance of the <see cref="NodeCheckSettings"/> class.</summary>
    public NodeCheckSettings()
    {
        this.ExecutablePath = "node";
    }
}
