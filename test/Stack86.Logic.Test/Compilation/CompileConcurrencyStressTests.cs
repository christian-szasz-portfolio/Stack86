namespace Stack86.Logic.Test.Compilation;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Concurrency regression coverage for the compilation pipeline. These tests guard
/// against the sync-over-async and pipe-buffer deadlocks that previously made the
/// external-tool stages block thread-pool threads under load. A regression would
/// surface here as a timeout (thread-pool starvation / deadlock) rather than a
/// functional assertion failure.
/// </summary>
[TestClass]
public sealed class CompileConcurrencyStressTests
{
    private const string ProgramSource = """
        #include <stdio.h>
        int main(void) {
            int total = 0;
            for (int i = 0; i < 5; i++) {
                total += i;
            }
            printf("%d", total);
            return 0;
        }
        """;

    [TestMethod]
    public async Task ManyParallelCompiles_AllSucceed_WithoutDeadlock()
    {
        const int parallelism = 32;

        var tasks = Enumerable
            .Range(0, parallelism)
            .Select(_ => CompileFixture.CompileC(ProgramSource))
            .ToArray();

        var completed = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));

        foreach (var result in completed)
        {
            Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : "expected success");
        }
    }

    [TestMethod]
    public async Task ManyParallelExternalValidations_CompleteQuickly_WhenToolMissing()
    {
        // Exercises TccCodeValidator.ValidateAsync concurrently. With the missing-binary
        // fallback each call returns a warning, but the point is that none of them block a
        // thread-pool thread: 64 concurrent calls must drain well within the timeout.
        const int parallelism = 64;

        var settings = new TccSettings
        {
            ExecutablePath = Path.Combine(Path.GetTempPath(), $"missing_tcc_{Guid.NewGuid():N}.exe"),
            MaxSourceSizeBytes = 65536,
        };
        var validator = new TccCodeValidator(settings);

        var tasks = Enumerable
            .Range(0, parallelism)
            .Select(_ => validator.ValidateAsync("int main(void) { return 0; }"))
            .ToArray();

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));

        foreach (var diags in results)
        {
            Assert.HasCount(1, diags);
            Assert.AreEqual(DiagnosticSeverity.Warning, diags[0].Severity);
        }
    }
}
