namespace Stack86.Test.Utility;

/// <summary>
/// Base class for unit tests that provides a System Under Test (SUT) instance with automatic
/// dependency injection from mocks.
/// </summary>
/// <typeparam name="TSut">The type of the system under test.</typeparam>
public abstract class TestBase<TSut> : TestBase
    where TSut : class
{
    private TSut? sut;

    /// <summary>Gets the lazily-created System Under Test with all dependencies resolved from mocks.</summary>
    protected TSut Sut => this.sut ??= this.CreateSut();

    /// <summary>Override to customize SUT construction.</summary>
    protected virtual TSut CreateSut() => this.MockFactory.Create<TSut>();

    /// <summary>Resets the SUT instance, causing it to be recreated on next access.</summary>
    protected void ResetSut() => this.sut = null;

    /// <inheritdoc/>
    protected override void OnCleanup()
    {
        this.sut = null;
        base.OnCleanup();
    }
}
