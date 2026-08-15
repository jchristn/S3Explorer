using S3Explorer.Models;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises <see cref="ActivityLogEntry"/> defaults and its <c>Display</c>
/// formatting (with and without progress), plus the <see cref="LogLevel"/> enum.
/// </summary>
public static class ActivityLogEntrySuite
{
    private const string Id = "ActivityLogEntry";

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "ActivityLogEntry",
        cases: new List<TestCaseDescriptor>
        {
            Case(Id, "defaults", "New entry has expected defaults", () =>
            {
                var entry = new ActivityLogEntry();
                Check.Equal("", entry.Message);
                Check.Equal(LogLevel.Info, entry.Level);
                Check.Null(entry.ProgressPercent);
                Check.False(entry.IsComplete);
                Check.NotEqual(default(DateTime), entry.Timestamp);
            }),

            Case(Id, "display-no-progress", "Display omits percent when no progress", () =>
            {
                var ts = new DateTime(2024, 3, 2, 13, 45, 9);
                var entry = new ActivityLogEntry { Timestamp = ts, Message = "Loading buckets" };
                Check.Equal($"[{ts:HH:mm:ss}] Loading buckets", entry.Display);
            }),

            Case(Id, "display-with-progress", "Display includes rounded percent", () =>
            {
                var ts = new DateTime(2024, 3, 2, 13, 45, 9);
                var entry = new ActivityLogEntry { Timestamp = ts, Message = "Uploading", ProgressPercent = 50 };
                Check.Equal($"[{ts:HH:mm:ss}] Uploading (50%)", entry.Display);
            }),

            Case(Id, "display-zero-progress", "Zero percent still renders '(0%)'", () =>
            {
                var ts = new DateTime(2024, 3, 2, 0, 0, 0);
                var entry = new ActivityLogEntry { Timestamp = ts, Message = "Start", ProgressPercent = 0 };
                Check.Equal($"[{ts:HH:mm:ss}] Start (0%)", entry.Display);
            }),

            Case(Id, "display-progress-rounding", "Progress percent rounds to whole number", () =>
            {
                var ts = new DateTime(2024, 3, 2, 0, 0, 0);
                var entry = new ActivityLogEntry { Timestamp = ts, Message = "Go", ProgressPercent = 49.6 };
                Check.Equal($"[{ts:HH:mm:ss}] Go (50%)", entry.Display);
            }),

            // LogLevel enum ordering.
            Case(Id, "loglevel-values", "LogLevel enum has expected ordinal values", () =>
            {
                Check.Equal(0, (int)LogLevel.Info);
                Check.Equal(1, (int)LogLevel.Success);
                Check.Equal(2, (int)LogLevel.Warning);
                Check.Equal(3, (int)LogLevel.Error);
            }),
        });
}
