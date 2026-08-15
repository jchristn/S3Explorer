using Test.Shared.Suites;
using Touchstone.Core;

namespace Test.Shared;

/// <summary>
/// The central source of truth for all S3Explorer tests. Every host — the
/// Touchstone CLI runner, the xUnit adapter, and the NUnit adapter — executes
/// exactly these suites, so the tests are defined once and run everywhere.
/// </summary>
public static class S3ExplorerTestSuites
{
    /// <summary>All test suites covering the S3Explorer platform.</summary>
    public static IReadOnlyList<TestSuiteDescriptor> All => new List<TestSuiteDescriptor>
    {
        FileSizeConverterSuite.Suite(),
        S3ObjectItemSuite.Suite(),
        ActivityLogEntrySuite.Suite(),
        S3AccountSuite.Suite(),
        ProgressStreamSuite.Suite(),
        S3ServiceUrlSuite.Suite(),
        S3ServiceListingSuite.Suite(),
        NavigationSuite.Suite(),
        AccountStoreSuite.Suite(),
    };
}
