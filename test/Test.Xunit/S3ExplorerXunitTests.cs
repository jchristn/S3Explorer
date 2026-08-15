using Test.Shared;
using Touchstone.Core;
using Touchstone.XunitAdapter;
using Xunit;

namespace Test.Xunit;

/// <summary>
/// xUnit host for the shared Touchstone suites using the theory-driven pattern:
/// each non-skipped <see cref="TestCaseDescriptor"/> becomes an individual xUnit
/// theory row. The suites themselves live in Test.Shared (the source of truth).
/// </summary>
public sealed class S3ExplorerXunitTests
{
    public static TouchstoneTheoryData TestCases() => new(S3ExplorerTestSuites.All);

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Run(TestCaseDescriptor testCase)
    {
        await testCase.ExecuteAsync(CancellationToken.None);
    }
}
