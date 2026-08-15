using Touchstone.Core;

namespace Test.Shared;

/// <summary>
/// Helpers for concisely building Touchstone <see cref="TestCaseDescriptor"/>
/// instances and for tests that need an isolated temporary directory.
/// </summary>
internal static class TestSupport
{
    /// <summary>Builds a test case from a synchronous body.</summary>
    public static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Action body)
        => new(suiteId, caseId, displayName, _ =>
        {
            body();
            return Task.CompletedTask;
        });

    /// <summary>Builds a test case from an asynchronous body.</summary>
    public static TestCaseDescriptor AsyncCase(string suiteId, string caseId, string displayName, Func<Task> body)
        => new(suiteId, caseId, displayName, _ => body());

    /// <summary>
    /// Creates a fresh, unique temporary directory, invokes <paramref name="body"/>
    /// with its path, and removes it afterwards. Used by persistence tests so they
    /// never touch the real per-user configuration directory.
    /// </summary>
    public static void WithTempDir(Action<string> body)
    {
        var dir = Path.Combine(Path.GetTempPath(), "S3ExplorerTests", Guid.NewGuid().ToString("N"));
        try
        {
            body(dir);
        }
        finally
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; a leaked temp dir must never fail a test.
            }
        }
    }
}
