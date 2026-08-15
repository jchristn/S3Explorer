using System.Collections;
using NUnit.Framework;
using Test.Shared;
using Touchstone.Core;
using Touchstone.NunitAdapter;

namespace Test.Nunit;

/// <summary>
/// NUnit host for the shared Touchstone suites using the TestCaseSource pattern:
/// each non-skipped <see cref="TestCaseDescriptor"/> becomes an individual NUnit
/// test case. The suites themselves live in Test.Shared (the source of truth).
/// </summary>
[TestFixture]
public sealed class S3ExplorerNunitTests
{
    private static IEnumerable TestCases() => new TouchstoneTestCaseSource(S3ExplorerTestSuites.All);

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public async Task Run(TestCaseDescriptor testCase)
    {
        await testCase.ExecuteAsync(CancellationToken.None);
    }
}
