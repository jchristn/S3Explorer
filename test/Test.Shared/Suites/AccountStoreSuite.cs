using S3Explorer.Models;
using S3Explorer.Services;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="AccountStore"/> persistence semantics against an isolated
/// temporary directory (never the real per-user config): load/save round-trips,
/// corrupt-file recovery, add/update/remove behavior, and disk persistence.
/// </summary>
public static class AccountStoreSuite
{
    private const string Id = "AccountStore";

    private static S3Account NewAccount(string name) =>
        new() { DisplayName = name, AccessKey = "AK", SecretKey = "SK", Region = "us-west-2" };

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "AccountStore",
        cases: new List<TestCaseDescriptor>
        {
            Case(Id, "load-missing", "Load with no file yields an empty list", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    store.Load();
                    Check.Empty(store.Accounts);
                })),

            Case(Id, "default-ctor-empty", "Default-constructed store starts empty (no disk write)", () =>
            {
                var store = new AccountStore();
                Check.Empty(store.Accounts);
            }),

            Case(Id, "save-load-roundtrip", "Saved accounts round-trip through a new store", () =>
                WithTempDir(dir =>
                {
                    var account = NewAccount("Prod");
                    var writer = new AccountStore(dir);
                    writer.AddOrUpdate(account);

                    var reader = new AccountStore(dir);
                    reader.Load();
                    Check.Count(1, reader.Accounts);
                    Check.Equal("Prod", reader.Accounts[0].DisplayName);
                    Check.Equal(account.Id, reader.Accounts[0].Id);
                    Check.Equal("us-west-2", reader.Accounts[0].Region);
                })),

            Case(Id, "roundtrip-all-fields", "Every account field survives a save/load round-trip", () =>
                WithTempDir(dir =>
                {
                    var account = new S3Account
                    {
                        DisplayName = "Full",
                        ServiceUrl = "minio.local:9000",
                        AccessKey = "AKIAEXAMPLE",
                        SecretKey = "s3cr3t/Key+Value",
                        Region = "eu-central-1",
                        ForcePathStyle = true, // non-default
                        UseSSL = false,        // non-default
                    };

                    var writer = new AccountStore(dir);
                    writer.AddOrUpdate(account);

                    var reader = new AccountStore(dir);
                    reader.Load();
                    Check.Count(1, reader.Accounts);
                    var loaded = reader.Accounts[0];
                    Check.Equal(account.Id, loaded.Id);
                    Check.Equal("Full", loaded.DisplayName);
                    Check.Equal("minio.local:9000", loaded.ServiceUrl);
                    Check.Equal("AKIAEXAMPLE", loaded.AccessKey);
                    Check.Equal("s3cr3t/Key+Value", loaded.SecretKey);
                    Check.Equal("eu-central-1", loaded.Region);
                    Check.True(loaded.ForcePathStyle, "ForcePathStyle should persist as true");
                    Check.False(loaded.UseSSL, "UseSSL should persist as false");
                })),

            Case(Id, "empty-file", "An empty config file recovers to an empty list", () =>
                WithTempDir(dir =>
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "accounts.json"), "");

                    var store = new AccountStore(dir);
                    store.Load();
                    Check.Empty(store.Accounts);
                })),

            Case(Id, "corrupt-json", "Corrupt config file recovers to an empty list", () =>
                WithTempDir(dir =>
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "accounts.json"), "{ this is not valid json ]");

                    var store = new AccountStore(dir);
                    store.Load();
                    Check.Empty(store.Accounts);
                })),

            Case(Id, "addorupdate-adds", "AddOrUpdate appends a new account", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    store.AddOrUpdate(NewAccount("One"));
                    Check.Count(1, store.Accounts);
                })),

            Case(Id, "addorupdate-updates", "AddOrUpdate replaces an existing account by Id", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    var account = NewAccount("Original");
                    store.AddOrUpdate(account);

                    var edited = new S3Account { Id = account.Id, DisplayName = "Renamed" };
                    store.AddOrUpdate(edited);

                    Check.Count(1, store.Accounts);
                    Check.Equal("Renamed", store.Accounts[0].DisplayName);
                })),

            Case(Id, "addorupdate-two-distinct", "Two distinct accounts both persist", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    store.AddOrUpdate(NewAccount("A"));
                    store.AddOrUpdate(NewAccount("B"));
                    Check.Count(2, store.Accounts);
                })),

            Case(Id, "remove-existing", "Remove deletes an account by Id", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    var account = NewAccount("Temp");
                    store.AddOrUpdate(account);
                    store.Remove(account.Id);
                    Check.Empty(store.Accounts);
                })),

            Case(Id, "remove-nonexistent", "Removing an unknown Id is a no-op", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    store.AddOrUpdate(NewAccount("Keep"));
                    store.Remove(Guid.NewGuid().ToString());
                    Check.Count(1, store.Accounts);
                })),

            Case(Id, "save-creates-directory", "Saving creates the config directory", () =>
                WithTempDir(dir =>
                {
                    Check.False(Directory.Exists(dir), "temp dir should not exist yet");
                    var store = new AccountStore(dir);
                    store.AddOrUpdate(NewAccount("X"));
                    Check.True(Directory.Exists(dir));
                    Check.True(File.Exists(Path.Combine(dir, "accounts.json")));
                })),

            Case(Id, "remove-persists", "Remove persists to disk", () =>
                WithTempDir(dir =>
                {
                    var store = new AccountStore(dir);
                    var a = NewAccount("A");
                    var b = NewAccount("B");
                    store.AddOrUpdate(a);
                    store.AddOrUpdate(b);
                    store.Remove(a.Id);

                    var reader = new AccountStore(dir);
                    reader.Load();
                    Check.Count(1, reader.Accounts);
                    Check.Equal("B", reader.Accounts[0].DisplayName);
                })),
        });
}
