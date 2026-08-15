using System.Globalization;
using S3Explorer.Models;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="S3ObjectItem"/> defaults and its computed display
/// properties (<c>FormattedSize</c>, <c>FormattedDate</c>) for both objects and
/// prefix (folder) rows.
/// </summary>
public static class S3ObjectItemSuite
{
    private const string Id = "S3ObjectItem";

    private static string Fmt(double v) => v.ToString("0.##", CultureInfo.CurrentCulture);

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "S3ObjectItem",
        cases: new List<TestCaseDescriptor>
        {
            // Defaults.
            Case(Id, "defaults", "New item has expected defaults", () =>
            {
                var item = new S3ObjectItem();
                Check.Equal("", item.Key);
                Check.Equal("", item.DisplayName);
                Check.Equal(0L, item.Size);
                Check.Null(item.LastModified);
                Check.Equal("", item.StorageClass);
                Check.False(item.IsPrefix);
                Check.Equal("", item.ETag);
            }),

            // FormattedSize for prefixes.
            Case(Id, "size-prefix", "Prefix row size shows '--'", () =>
            {
                var item = new S3ObjectItem { IsPrefix = true, Size = 4096 };
                Check.Equal("--", item.FormattedSize);
            }),

            // FormattedSize for objects.
            Case(Id, "size-zero", "Zero-byte object -> '0 B'", () =>
                Check.Equal("0 B", new S3ObjectItem { Size = 0 }.FormattedSize)),
            Case(Id, "size-kb", "2048 bytes -> '2 KB'", () =>
                Check.Equal("2 KB", new S3ObjectItem { Size = 2048 }.FormattedSize)),
            Case(Id, "size-fractional", "1536 bytes -> '1.5 KB'", () =>
                Check.Equal($"{Fmt(1.5)} KB", new S3ObjectItem { Size = 1536 }.FormattedSize)),
            Case(Id, "size-tb", "1 TB object -> '1 TB'", () =>
                Check.Equal("1 TB", new S3ObjectItem { Size = 1024L * 1024 * 1024 * 1024 }.FormattedSize)),

            // FormattedDate for prefixes.
            Case(Id, "date-prefix", "Prefix row date shows '--'", () =>
            {
                var item = new S3ObjectItem { IsPrefix = true, LastModified = new DateTime(2020, 1, 1) };
                Check.Equal("--", item.FormattedDate);
            }),

            // FormattedDate when no modification time.
            Case(Id, "date-null", "Object with null LastModified -> '--'", () =>
                Check.Equal("--", new S3ObjectItem { LastModified = null }.FormattedDate)),

            // FormattedDate formatting.
            Case(Id, "date-formatted", "LastModified formatted as yyyy-MM-dd HH:mm:ss", () =>
            {
                var when = new DateTime(2023, 7, 14, 9, 5, 3);
                var item = new S3ObjectItem { LastModified = when };
                Check.Equal(when.ToString("yyyy-MM-dd HH:mm:ss"), item.FormattedDate);
            }),

            // Default computed properties.
            Case(Id, "default-formatted-size", "Default item FormattedSize -> '0 B'", () =>
                Check.Equal("0 B", new S3ObjectItem().FormattedSize)),
            Case(Id, "default-formatted-date", "Default item FormattedDate -> '--'", () =>
                Check.Equal("--", new S3ObjectItem().FormattedDate)),
        });
}
