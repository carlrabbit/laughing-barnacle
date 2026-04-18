namespace Binstore;

internal sealed class ChunkedReadStream(IReadOnlyList<byte[]> chunks) : Stream
{
    private int chunkIndex;
    private int chunkOffset;

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

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

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

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(Read(buffer, offset, count));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : ValueTask.FromResult(Read(buffer.Span));

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
