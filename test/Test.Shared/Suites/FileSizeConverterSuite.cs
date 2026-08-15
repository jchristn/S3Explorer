using System.Globalization;
using S3Explorer.Converters;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="FileSizeConverter"/> across unit boundaries, rounding,
/// non-numeric inputs, and the unsupported reverse conversion.
/// </summary>
public static class FileSizeConverterSuite
{
    private const string Id = "FileSizeConverter";

    // The converter formats with the current culture, so decimal expectations are
    // derived with the same format string to stay correct on any machine culture.
    private static string Fmt(double v) => v.ToString("0.##", CultureInfo.CurrentCulture);

    private static string Convert(object? value)
        => (string)FileSizeConverter.Instance.Convert(
            value, typeof(string), null, CultureInfo.InvariantCulture)!;

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "FileSizeConverter",
        cases: new List<TestCaseDescriptor>
        {
            Case(Id, "instance-singleton", "Instance is a non-null singleton", () =>
            {
                Check.NotNull(FileSizeConverter.Instance);
                Check.True(ReferenceEquals(FileSizeConverter.Instance, FileSizeConverter.Instance));
            }),

            // Positive: byte-range values (no unit escalation).
            Case(Id, "zero-bytes", "0 bytes -> '0 B'", () => Check.Equal("0 B", Convert(0L))),
            Case(Id, "sub-kb", "512 bytes -> '512 B'", () => Check.Equal("512 B", Convert(512L))),
            Case(Id, "max-bytes", "1023 bytes -> '1023 B'", () => Check.Equal("1023 B", Convert(1023L))),

            // Positive: exact unit boundaries.
            Case(Id, "one-kb", "1024 bytes -> '1 KB'", () => Check.Equal("1 KB", Convert(1024L))),
            Case(Id, "one-mb", "1048576 bytes -> '1 MB'", () => Check.Equal("1 MB", Convert(1024L * 1024))),
            Case(Id, "one-gb", "1073741824 bytes -> '1 GB'", () => Check.Equal("1 GB", Convert(1024L * 1024 * 1024))),
            Case(Id, "one-tb", "1099511627776 bytes -> '1 TB'", () => Check.Equal("1 TB", Convert(1024L * 1024 * 1024 * 1024))),

            // Positive: fractional values with rounding to two decimals.
            Case(Id, "one-and-half-kb", "1536 bytes -> '1.5 KB'", () => Check.Equal($"{Fmt(1.5)} KB", Convert(1536L))),
            Case(Id, "rounded-kb", "1500 bytes rounds to 2 decimals", () => Check.Equal($"{Fmt(1500 / 1024.0)} KB", Convert(1500L))),
            Case(Id, "rounded-mb", "5_400_000 bytes -> MB rounded", () => Check.Equal($"{Fmt(5_400_000 / 1024.0 / 1024.0)} MB", Convert(5_400_000L))),

            // Positive: caps at the largest unit (TB) rather than overflowing.
            Case(Id, "caps-at-tb", "Beyond 1 TB stays in TB", () =>
            {
                var bytes = 1024L * 1024 * 1024 * 1024 * 1024; // 1024 TB
                Check.Equal($"{Fmt(1024)} TB", Convert(bytes));
            }),

            // Negative / edge: negative numbers never escalate units.
            Case(Id, "negative", "Negative value stays in bytes", () => Check.Equal("-500 B", Convert(-500L))),

            // Negative: non-long numeric types are not handled and fall back to ToString().
            Case(Id, "int-fallback", "int (not long) falls back to ToString()", () => Check.Equal("42", Convert(42))),
            Case(Id, "double-fallback", "double falls back to ToString()", () => Check.Equal((1024.0).ToString(CultureInfo.CurrentCulture), Convert(1024.0))),

            // Negative: non-numeric inputs.
            Case(Id, "string-fallback", "string value returned as-is", () => Check.Equal("hello", Convert("hello"))),
            Case(Id, "null-input", "null value -> empty string", () => Check.Equal("", Convert(null))),

            // Negative: reverse conversion is unsupported.
            Case(Id, "convert-back-throws", "ConvertBack throws NotImplementedException", () =>
                Check.Throws<NotImplementedException>(() =>
                    FileSizeConverter.Instance.ConvertBack("x", typeof(long), null, CultureInfo.InvariantCulture))),
        });
}
