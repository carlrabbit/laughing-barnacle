namespace Jsonstore;
/// <summary>
/// Represents chunked read stream.
/// </summary>

internal sealed class ChunkedReadStream(IAsyncEnumerator<byte[]> chunkEnumerator) : Stream
{
    private readonly IAsyncEnumerator<byte[]> chunkEnumerator = chunkEnumerator;
    private byte[]? currentChunk;
    private int currentOffset;
    private bool isCompleted;
    /// <summary>
    /// Performs the not supported exception operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    public override bool CanRead => true;
    /// <summary>
    /// Performs the not supported exception operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public override bool CanSeek => false;
    /// <summary>
    /// Performs the not supported exception operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public override bool CanWrite => false;
    /// <summary>
    /// Performs the not supported exception operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public override long Length => throw new NotSupportedException();
    /// <summary>
    /// Performs the not supported exception operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    /// <summary>
    /// Performs the flush operation.
    /// </summary>

    public override void Flush()
    {
    }
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <returns>The operation result.</returns>

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Use ReadAsync for streaming JSON chunks.");
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <returns>The operation result.</returns>

    public override int Read(Span<byte> buffer) =>
        throw new NotSupportedException("Use ReadAsync for streaming JSON chunks.");
    /// <summary>
    /// Performs the read async operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    /// <summary>
    /// Performs the read async operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

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
    /// <summary>
    /// Performs the seek operation.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="origin">The origin.</param>
    /// <returns>The operation result.</returns>

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <summary>
    /// Performs the set length operation.
    /// </summary>

    public override void SetLength(long value) => throw new NotSupportedException();
    /// <summary>
    /// Performs the write operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    /// <summary>
    /// Performs the dispose async operation.
    /// </summary>
    /// <returns>The operation result.</returns>

    public override async ValueTask DisposeAsync()
    {
        await chunkEnumerator.DisposeAsync();
        await base.DisposeAsync();
    }
    /// <summary>
    /// Performs the dispose operation.
    /// </summary>
    /// <param name="disposing">The disposing.</param>

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
