namespace Binstore;
/// <summary>
/// Represents chunk sizing.
/// </summary>

internal static class ChunkSizing
{
    /// <summary>
    /// Chunk size used when the payload is smaller than 1 MB.
    /// </summary>
    public const int Under1Mb = 64 * 1024;

    /// <summary>
    /// Chunk size used when the payload is between 1 MB and 10 MB.
    /// </summary>
    public const int Under10Mb = 256 * 1024;

    /// <summary>
    /// Chunk size used when the payload is between 10 MB and 100 MB.
    /// </summary>
    public const int Under100Mb = 1024 * 1024;

    /// <summary>
    /// Chunk size used when the payload is larger than 100 MB.
    /// </summary>
    public const int Over100Mb = 4 * 1024 * 1024;

    /// <summary>
    /// Gets the chunk size for a payload length.
    /// </summary>
    /// <param name="contentLength">The content length.</param>
    /// <returns>The chunk size in bytes.</returns>

    public static int GetChunkSize(long? contentLength) => contentLength switch
    {
        null => Under10Mb,
        < 1024L * 1024L => Under1Mb,
        < 10L * 1024L * 1024L => Under10Mb,
        < 100L * 1024L * 1024L => Under100Mb,
        _ => Over100Mb
    };
}
