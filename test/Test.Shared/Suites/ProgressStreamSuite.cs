using System.Text;
using S3Explorer.Services;
using Touchstone.Core;
using static Test.Shared.TestSupport;

namespace Test.Shared.Suites;

/// <summary>
/// Exercises the internal <see cref="ProgressStream"/> wrapper: capability and
/// property pass-through plus cumulative progress reporting on read paths.
/// (Visible to this assembly via <c>InternalsVisibleTo</c>.)
/// </summary>
public static class ProgressStreamSuite
{
    private const string Id = "ProgressStream";

    private static MemoryStream Data(int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++) bytes[i] = (byte)(i & 0xFF);
        return new MemoryStream(bytes, writable: false);
    }

    public static TestSuiteDescriptor Suite() => new(
        suiteId: Id,
        displayName: "ProgressStream",
        cases: new List<TestCaseDescriptor>
        {
            Case(Id, "capabilities-passthrough", "Can* flags reflect the inner stream", () =>
            {
                using var inner = new MemoryStream();
                using var ps = new ProgressStream(inner, _ => { });
                Check.Equal(inner.CanRead, ps.CanRead);
                Check.Equal(inner.CanWrite, ps.CanWrite);
                Check.Equal(inner.CanSeek, ps.CanSeek);
            }),

            Case(Id, "length-passthrough", "Length reflects the inner stream", () =>
            {
                using var inner = Data(123);
                using var ps = new ProgressStream(inner, _ => { });
                Check.Equal(123L, ps.Length);
            }),

            Case(Id, "position-get-set", "Position get/set delegates to inner", () =>
            {
                using var inner = Data(100);
                using var ps = new ProgressStream(inner, _ => { });
                ps.Position = 40;
                Check.Equal(40L, ps.Position);
                Check.Equal(40L, inner.Position);
            }),

            Case(Id, "read-reports-cumulative", "Read reports cumulative bytes read", () =>
            {
                using var inner = Data(10);
                long last = -1;
                using var ps = new ProgressStream(inner, total => last = total);

                var buffer = new byte[4];
                Check.Equal(4, ps.Read(buffer, 0, 4));
                Check.Equal(4L, last);
                Check.Equal(4, ps.Read(buffer, 0, 4));
                Check.Equal(8L, last);
                Check.Equal(2, ps.Read(buffer, 0, 4));
                Check.Equal(10L, last);
                // Reading past the end returns 0 and leaves the total unchanged.
                Check.Equal(0, ps.Read(buffer, 0, 4));
                Check.Equal(10L, last);
            }),

            AsyncCase(Id, "read-async-reports-cumulative", "ReadAsync reports cumulative bytes read", async () =>
            {
                using var inner = Data(6);
                long last = -1;
                using var ps = new ProgressStream(inner, total => last = total);

                var buffer = new byte[4];
                Check.Equal(4, await ps.ReadAsync(buffer, 0, 4));
                Check.Equal(4L, last);
                Check.Equal(2, await ps.ReadAsync(buffer, 0, 4));
                Check.Equal(6L, last);
            }),

            // The Memory<byte> ReadAsync overload is the one the download path uses
            // (Stream.ReadAsync(Memory<byte>)); it must still surface cumulative progress
            // even though ProgressStream only overrides the array-based overload.
            AsyncCase(Id, "read-async-memory-reports-cumulative", "ReadAsync(Memory<byte>) reports cumulative bytes read", async () =>
            {
                using var inner = Data(6);
                long last = -1;
                using var ps = new ProgressStream(inner, total => last = total);

                var buffer = new byte[4];
                Check.Equal(4, await ps.ReadAsync(buffer.AsMemory(0, 4)));
                Check.Equal(4L, last);
                Check.Equal(2, await ps.ReadAsync(buffer.AsMemory(0, 4)));
                Check.Equal(6L, last);
                // Reading past the end reports 0 and leaves the total unchanged.
                Check.Equal(0, await ps.ReadAsync(buffer.AsMemory(0, 4)));
                Check.Equal(6L, last);
            }),

            // Reads into a non-zero buffer offset must still count only the bytes read.
            Case(Id, "read-honors-offset", "Read into a non-zero buffer offset reports bytes read", () =>
            {
                using var inner = Data(5);
                long last = -1;
                using var ps = new ProgressStream(inner, total => last = total);

                var buffer = new byte[10];
                // Data(5) yields bytes [0,1,2,3,4]; read the first three into offset 4.
                Check.Equal(3, ps.Read(buffer, 4, 3));
                Check.Equal(3L, last);
                // Bytes landed at the requested offset (buffer[4..6] = 0,1,2)...
                Check.Equal((byte)1, buffer[5]);
                Check.Equal((byte)2, buffer[6]);
                // ...and nothing was written before the offset or past the count.
                Check.Equal((byte)0, buffer[3]);
                Check.Equal((byte)0, buffer[7]);
            }),

            Case(Id, "seek-delegates", "Seek delegates to inner and moves position", () =>
            {
                using var inner = Data(50);
                using var ps = new ProgressStream(inner, _ => { });
                var pos = ps.Seek(10, SeekOrigin.Begin);
                Check.Equal(10L, pos);
                Check.Equal(10L, inner.Position);
            }),

            Case(Id, "write-delegates", "Write delegates to inner stream", () =>
            {
                using var inner = new MemoryStream();
                using var ps = new ProgressStream(inner, _ => { });
                var payload = Encoding.ASCII.GetBytes("hello");
                ps.Write(payload, 0, payload.Length);
                Check.Equal(5L, inner.Length);
                Check.Equal("hello", Encoding.ASCII.GetString(inner.ToArray()));
            }),

            Case(Id, "flush-no-throw", "Flush delegates without throwing", () =>
            {
                using var inner = new MemoryStream();
                using var ps = new ProgressStream(inner, _ => { });
                ps.Write(new byte[] { 1, 2, 3 }, 0, 3);
                ps.Flush(); // must not throw
                Check.Equal(3L, inner.Length);
            }),

            Case(Id, "set-length-delegates", "SetLength delegates to inner", () =>
            {
                using var inner = new MemoryStream();
                using var ps = new ProgressStream(inner, _ => { });
                ps.SetLength(64);
                Check.Equal(64L, inner.Length);
            }),
        });
}
