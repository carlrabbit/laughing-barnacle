namespace Jsonstore;

internal static class ChunkSizing
{
    public const int Under1Mb = 64 * 1024;
    public const int Under10Mb = 256 * 1024;
    public const int Under100Mb = 1024 * 1024;
    public const int Over100Mb = 4 * 1024 * 1024;

    public static int GetChunkSize(long? contentLength) => contentLength switch
    {
        null => Under10Mb,
        < 1024L * 1024L => Under1Mb,
        < 10L * 1024L * 1024L => Under10Mb,
        < 100L * 1024L * 1024L => Under100Mb,
        _ => Over100Mb
    };
}
