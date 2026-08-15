using S3Explorer.Models;
using S3Explorer.Services;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="S3Service.BuildServiceUrl"/>: scheme selection from the
/// SSL preference, scheme preservation when already present, and the "no custom
/// endpoint" (null) result. (Internal, visible via <c>InternalsVisibleTo</c>.)
/// </summary>
public static class S3ServiceUrlSuite
{
    private const string Id = "S3ServiceUrl";

    private static S3Account Account(string serviceUrl, bool useSsl) =>
        new() { ServiceUrl = serviceUrl, UseSSL = useSsl };

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "S3Service.BuildServiceUrl",
        cases: new List<TestCaseDescriptor>
        {
            // Negative: no custom endpoint configured -> null (use AWS default).
            Case(Id, "empty-url", "Empty ServiceUrl -> null", () =>
                Check.Null(S3Service.BuildServiceUrl(Account("", true)))),
            Case(Id, "whitespace-url", "Whitespace ServiceUrl -> null", () =>
                Check.Null(S3Service.BuildServiceUrl(Account("   ", true)))),

            // Positive: scheme derived from SSL preference when absent.
            Case(Id, "adds-https", "Schemeless URL with SSL -> https://", () =>
                Check.Equal("https://localhost:9000", S3Service.BuildServiceUrl(Account("localhost:9000", true)))),
            Case(Id, "adds-http", "Schemeless URL without SSL -> http://", () =>
                Check.Equal("http://localhost:9000", S3Service.BuildServiceUrl(Account("localhost:9000", false)))),
            Case(Id, "ip-host-http", "Schemeless IP:port without SSL -> http://", () =>
                Check.Equal("http://192.168.1.10:9000", S3Service.BuildServiceUrl(Account("192.168.1.10:9000", false)))),

            // Positive: existing scheme is preserved regardless of SSL preference.
            Case(Id, "keeps-http", "Existing http:// preserved even with SSL on", () =>
                Check.Equal("http://localhost:9000", S3Service.BuildServiceUrl(Account("http://localhost:9000", true)))),
            Case(Id, "keeps-https", "Existing https:// preserved even with SSL off", () =>
                Check.Equal("https://s3.example.com", S3Service.BuildServiceUrl(Account("https://s3.example.com", false)))),
        });
}
