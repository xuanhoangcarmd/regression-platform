using System.IO.Compression;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// GZip helpers for the Data Library: payloads/raw buffers are stored compressed (mostly zero-padded,
/// so they shrink dramatically) and decompressed when read back.
/// </summary>
internal static class GzipCompressor
{
    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>Decodes stored bytes to base64, decompressing first when <paramref name="compression"/> is gzip.</summary>
    public static string ToBase64(byte[] stored, string? compression)
    {
        var raw = string.Equals(compression, "gzip", StringComparison.OrdinalIgnoreCase)
            ? Decompress(stored)
            : stored;
        return Convert.ToBase64String(raw);
    }
}
