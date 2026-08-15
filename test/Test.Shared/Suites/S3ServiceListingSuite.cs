using S3Explorer.Models;
using S3Explorer.Services;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="S3Service.BuildListing"/>, the object-listing
/// normalization: common-prefix collapsing/de-duplication, folder-marker and
/// self-prefix filtering, display-name derivation, metadata mapping, and sort
/// order. (Internal, visible via <c>InternalsVisibleTo</c>.)
/// </summary>
public static class S3ServiceListingSuite
{
    private const string Id = "S3ServiceListing";

    private static S3Service.RawS3Object Obj(
        string key, long size = 0, DateTime? modified = null,
        string? storageClass = null, string? etag = null)
        => new(key, size, modified, storageClass, etag);

    private static List<S3ObjectItem> Build(
        string prefix,
        IEnumerable<string>? prefixes = null,
        IEnumerable<S3Service.RawS3Object>? objects = null)
        => S3Service.BuildListing(
            prefix,
            prefixes ?? Array.Empty<string>(),
            objects ?? Array.Empty<S3Service.RawS3Object>());

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "S3Service.BuildListing",
        cases: new List<TestCaseDescriptor>
        {
            // Empty inputs.
            Case(Id, "empty", "No prefixes or objects -> empty list", () =>
                Check.Empty(Build(""))),

            // Single object at the root.
            Case(Id, "single-object", "Single root object maps correctly", () =>
            {
                var when = new DateTime(2022, 5, 1, 8, 0, 0);
                var items = Build("", objects: new[] { Obj("file.txt", 100, when, "STANDARD", "\"abc\"") });
                Check.Count(1, items);
                var item = items[0];
                Check.Equal("file.txt", item.Key);
                Check.Equal("file.txt", item.DisplayName);
                Check.Equal(100L, item.Size);
                Check.Equal(when, item.LastModified);
                Check.Equal("STANDARD", item.StorageClass);
                Check.Equal("\"abc\"", item.ETag);
                Check.False(item.IsPrefix);
            }),

            // Null metadata mapped to empty strings.
            Case(Id, "null-metadata", "Null storage class / etag map to empty strings", () =>
            {
                var items = Build("", objects: new[] { Obj("a.bin") });
                Check.Equal("", items[0].StorageClass);
                Check.Equal("", items[0].ETag);
            }),

            // Object whose key equals the current prefix is dropped.
            Case(Id, "skip-self-prefix", "Object equal to current prefix is skipped", () =>
                Check.Empty(Build("folder/", objects: new[] { Obj("folder/") }))),

            // Folder-marker objects (trailing slash) are dropped.
            Case(Id, "skip-folder-marker", "Object ending in '/' is skipped", () =>
                Check.Empty(Build("", objects: new[] { Obj("folder/sub/") }))),

            // Nested object key -> display name is the last segment.
            Case(Id, "nested-object-display", "Nested key display name is last segment", () =>
            {
                var items = Build("", objects: new[] { Obj("a/b/c.txt", 5) });
                Check.Count(1, items);
                Check.Equal("a/b/c.txt", items[0].Key);
                Check.Equal("c.txt", items[0].DisplayName);
            }),

            // Object under a prefix -> display name is last segment.
            Case(Id, "prefixed-object-display", "Object under prefix shows last segment", () =>
            {
                var items = Build("docs/", objects: new[] { Obj("docs/readme.md", 12) });
                Check.Count(1, items);
                Check.Equal("readme.md", items[0].DisplayName);
                Check.Equal("docs/readme.md", items[0].Key);
            }),

            // Common prefix at root.
            Case(Id, "common-prefix-root", "Common prefix becomes a folder row", () =>
            {
                var items = Build("", prefixes: new[] { "photos/" });
                Check.Count(1, items);
                Check.True(items[0].IsPrefix);
                Check.Equal("photos/", items[0].Key);
                Check.Equal("photos/", items[0].DisplayName);
            }),

            // Deeply nested common prefix collapses to one level.
            Case(Id, "common-prefix-collapse", "Deep common prefix collapses to one level", () =>
            {
                var items = Build("", prefixes: new[] { "test/test2/test3/" });
                Check.Count(1, items);
                Check.Equal("test/", items[0].Key);
                Check.Equal("test/", items[0].DisplayName);
            }),

            // Collapse honors the current prefix.
            Case(Id, "common-prefix-collapse-with-prefix", "Deep prefix collapses relative to current prefix", () =>
            {
                var items = Build("docs/", prefixes: new[] { "docs/2020/reports/" });
                Check.Count(1, items);
                Check.Equal("docs/2020/", items[0].Key);
                Check.Equal("2020/", items[0].DisplayName);
            }),

            // Common prefix equal to the current prefix is dropped.
            Case(Id, "skip-self-common-prefix", "Common prefix equal to current prefix is skipped", () =>
                Check.Empty(Build("folder/", prefixes: new[] { "folder/" }))),

            // Duplicate/collapsing common prefixes are de-duplicated.
            Case(Id, "dedup-common-prefixes", "Prefixes collapsing to the same folder are de-duplicated", () =>
            {
                var items = Build("", prefixes: new[] { "test/a/", "test/b/", "test/c/" });
                Check.Count(1, items);
                Check.Equal("test/", items[0].DisplayName);
            }),

            // A bare "/" common prefix yields an empty display name and is dropped.
            Case(Id, "skip-empty-display-prefix", "Common prefix '/' yields empty name and is skipped", () =>
                Check.Empty(Build("", prefixes: new[] { "/" }))),

            // Objects are NOT de-duplicated (mirrors real listing semantics).
            Case(Id, "objects-not-deduped", "Repeated object keys are all retained", () =>
            {
                var items = Build("", objects: new[] { Obj("dup.txt"), Obj("dup.txt") });
                Check.Count(2, items);
            }),

            // Sort order: folders first, then alphabetical by display name.
            Case(Id, "sort-folders-first", "Folders sort before objects, then alphabetical", () =>
            {
                var items = Build("",
                    prefixes: new[] { "mid/" },
                    objects: new[] { Obj("b.txt"), Obj("a.txt") });
                Check.Count(3, items);
                Check.Equal("mid/", items[0].DisplayName);
                Check.True(items[0].IsPrefix);
                Check.Equal("a.txt", items[1].DisplayName);
                Check.Equal("b.txt", items[2].DisplayName);
            }),

            Case(Id, "sort-prefixes-alpha", "Multiple folders sort alphabetically", () =>
            {
                var items = Build("", prefixes: new[] { "zebra/", "apple/", "mango/" });
                Check.Count(3, items);
                Check.Equal("apple/", items[0].DisplayName);
                Check.Equal("mango/", items[1].DisplayName);
                Check.Equal("zebra/", items[2].DisplayName);
            }),

            // Full realistic mix, including a folder marker and self-prefix to be filtered.
            Case(Id, "mixed-listing", "Mixed listing filters, maps, and sorts correctly", () =>
            {
                var items = Build("app/",
                    prefixes: new[] { "app/logs/", "app/logs/", "app/data/" },
                    objects: new[]
                    {
                        Obj("app/"),              // self-prefix -> skipped
                        Obj("app/keep/"),         // folder marker -> skipped
                        Obj("app/readme.txt", 3),
                        Obj("app/config.json", 9),
                    });

                // Two folders (deduped logs/, data/) + two objects.
                Check.Count(4, items);
                Check.Equal("data/", items[0].DisplayName);
                Check.Equal("logs/", items[1].DisplayName);
                Check.Equal("config.json", items[2].DisplayName);
                Check.Equal("readme.txt", items[3].DisplayName);
            }),
        });
}
