namespace Binstore;
/// <summary>
/// Represents chunked read stream.
/// </summary>

internal sealed class ChunkedReadStream(IReadOnlyList<byte[]> chunks) : Stream
{
    private int chunkIndex;
    private int chunkOffset;
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

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <returns>The operation result.</returns>

    public override int Read(Span<byte> buffer)
    {
        var bytesCopied = 0;

        while (buffer.Length > 0 && chunkIndex < chunks.Count)
        {
            var chunk = chunks[chunkIndex];
            var remainingInChunk = chunk.Length - chunkOffset;
            if (remainingInChunk == 0)
            {
                chunkIndex++;
                chunkOffset = 0;
                continue;
            }

            var copyLength = Math.Min(buffer.Length, remainingInChunk);
            chunk.AsSpan(chunkOffset, copyLength).CopyTo(buffer);
            buffer = buffer[copyLength..];
            chunkOffset += copyLength;
            bytesCopied += copyLength;
        }

        return bytesCopied;
    }
    /// <summary>
    /// Performs the read async operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(Read(buffer, offset, count));
    /// <summary>
    /// Performs the read async operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : ValueTask.FromResult(Read(buffer.Span));
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
}
