using S3Explorer.Models;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="S3Account"/> defaults, identity generation, and its
/// <c>ToString()</c> override.
/// </summary>
public static class S3AccountSuite
{
    private const string Id = "S3Account";

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "S3Account",
        cases: new List<TestCaseDescriptor>
        {
            Case(Id, "defaults", "New account has expected defaults", () =>
            {
                var account = new S3Account();
                Check.Equal("", account.DisplayName);
                Check.Equal("", account.ServiceUrl);
                Check.Equal("", account.AccessKey);
                Check.Equal("", account.SecretKey);
                Check.Equal("us-east-1", account.Region);
                Check.False(account.ForcePathStyle);
                Check.True(account.UseSSL);
            }),

            Case(Id, "id-is-guid", "Default Id is a parseable GUID", () =>
            {
                var account = new S3Account();
                Check.True(Guid.TryParse(account.Id, out _), $"Id was not a GUID: '{account.Id}'");
            }),

            Case(Id, "ids-unique", "Two accounts get distinct Ids", () =>
            {
                Check.NotEqual(new S3Account().Id, new S3Account().Id);
            }),

            Case(Id, "tostring-displayname", "ToString returns DisplayName", () =>
            {
                var account = new S3Account { DisplayName = "Production" };
                Check.Equal("Production", account.ToString());
            }),

            Case(Id, "tostring-default-empty", "ToString on default account is empty", () =>
            {
                Check.Equal("", new S3Account().ToString());
            }),
        });
}
