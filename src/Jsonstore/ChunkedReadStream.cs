namespace Jsonstore;

internal sealed class ChunkedReadStream(IAsyncEnumerator<byte[]> chunkEnumerator) : Stream
{
    private readonly IAsyncEnumerator<byte[]> chunkEnumerator = chunkEnumerator;
    private byte[]? currentChunk;
    private int currentOffset;
    private bool isCompleted;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Use ReadAsync for streaming JSON chunks.");

    public override int Read(Span<byte> buffer) =>
        throw new NotSupportedException("Use ReadAsync for streaming JSON chunks.");

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await ValueTask.FromCanceled<int>(cancellationToken);
        }

        var bytesCopied = 0;

        while (!buffer.IsEmpty)
        {
            if (!await EnsureCurrentChunkAsync(cancellationToken))
            {
                break;
            }

            var remaining = currentChunk!.Length - currentOffset;
            var copyLength = Math.Min(buffer.Length, remaining);
            currentChunk.AsMemory(currentOffset, copyLength).CopyTo(buffer);
            currentOffset += copyLength;
            bytesCopied += copyLength;
            buffer = buffer[copyLength..];
        }

        return bytesCopied;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask DisposeAsync()
    {
        await chunkEnumerator.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            chunkEnumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }

    private async Task<bool> EnsureCurrentChunkAsync(CancellationToken cancellationToken)
    {
        if (isCompleted)
        {
            return false;
        }

        while (currentChunk is null || currentOffset >= currentChunk.Length)
        {
            if (!await chunkEnumerator.MoveNextAsync())
            {
                isCompleted = true;
                currentChunk = null;
                return false;
            }

            currentChunk = chunkEnumerator.Current;
            currentOffset = 0;
            if (currentChunk.Length > 0)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return true;
    }
}
