namespace Stack86.Test.Utility;

using Microsoft.Extensions.DependencyInjection;
using Stack86.Test.Utility.Mocking;

/// <summary>
/// Base class for unit tests providing mock management capabilities.
/// </summary>
public abstract class TestBase
{
    private MockFactory? mockFactory;

    /// <summary>Gets or sets the mock factory used to create and manage mocks.</summary>
    [Inject]
    public MockFactory MockFactory
    {
        get
        {
            if (this.mockFactory == null)
            {
                this.mockFactory = new MockFactory();
                this.mockFactory.InjectProperties(this);
            }

            return this.mockFactory;
        }

        set => this.mockFactory = value;
    }

    /// <summary>Gets the context information for the current test execution.</summary>
    public required TestContext TestContext { get; init; }

    [TestCleanup]
    public void CleanupTestBase()
    {
        this.OnCleanup();
        this.mockFactory?.Clear();
        this.mockFactory = null;
    }

    /// <summary>Gets or creates a mock for the specified interface or class type.</summary>
    protected Moq.Mock<T> GetMock<T>()
        where T : class => this.MockFactory.GetMock<T>();

    /// <summary>Gets the mocked object instance for the specified type.</summary>
    protected T GetObject<T>()
        where T : class => this.MockFactory.GetObject<T>();

    /// <summary>Verifies all expectations on all mocks created by this factory.</summary>
    protected void VerifyAll() => this.MockFactory.VerifyAll();

    /// <summary>Verifies all verifiable expectations on all mocks have been met.</summary>
    protected void Verify() => this.MockFactory.Verify();

    /// <summary>Override to perform custom cleanup logic before mocks are cleared.</summary>
    protected virtual void OnCleanup()
    {
    }
}
