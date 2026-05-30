using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Jsonstore;
/// <summary>
/// Represents write once json store.
/// </summary>

public sealed class WriteOnceJsonStore(JsonStoreDbContext dbContext) : IWriteOnceJsonStore
{
    private const int ReadBufferSize = 16 * 1024;
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="json">The json.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public Task<JsonStoreResult> StoreAsync(string key, string json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        return StoreAsync(key, new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false), cancellationToken);
    }
    /// <summary>
    /// Performs the store async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="jsonStream">The json stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async Task<JsonStoreResult> StoreAsync(
        string key,
        Stream jsonStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);
        if (!jsonStream.CanRead)
        {
            throw new ArgumentException("JSON stream must be readable.", nameof(jsonStream));
        }

        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var existing = await dbContext.Jsons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (existing is not null)
        {
            return new JsonStoreResult(false, existing.Id, existing.JsonType, existing.TotalBytes, existing.ChunkCount);
        }

        var record = new JsonRecord
        {
            Key = key,
            KeyHash = keyHash,
            JsonType = JsonRootType.Object,
            CreatedUtc = DateTimeOffset.UtcNow,
            TotalBytes = 0,
            ChunkCount = 0
        };

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        dbContext.Jsons.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            var chunkSize = ChunkSizing.GetChunkSize(jsonStream.CanSeek ? jsonStream.Length - jsonStream.Position : null);
            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
            try
            {
                JsonRootType? jsonType = null;
                var bomState = 0;
                var chunkIndex = 0;
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await jsonStream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)) > 0)
                {
                    ValidateAndDetectJsonRootType(buffer.AsSpan(0, bytesRead), ref jsonType, ref bomState);

                    dbContext.JsonChunks.Add(new JsonChunkRecord
                    {
                        JsonId = record.Id,
                        ChunkIndex = chunkIndex,
                        Data = buffer[..bytesRead].ToArray()
                    });

                    chunkIndex++;
                    totalBytes += bytesRead;
                }

                if (jsonType is null)
                {
                    throw new JsonException("JSON payload cannot be empty.");
                }

                record.JsonType = jsonType.Value;
                record.TotalBytes = totalBytes;
                record.ChunkCount = chunkIndex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            var current = await dbContext.Jsons
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
            if (current is not null)
            {
                return new JsonStoreResult(false, current.Id, current.JsonType, current.TotalBytes, current.ChunkCount);
            }

            throw;
        }

        return new JsonStoreResult(true, record.Id, record.JsonType, record.TotalBytes, record.ChunkCount);
    }
    /// <summary>
    /// Performs the get stream async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        var record = await dbContext.Jsons
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.KeyHash == keyHash && x.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var chunkEnumerator = dbContext.JsonChunks
            .AsNoTracking()
            .Where(x => x.JsonId == record.Id)
            .OrderBy(x => x.ChunkIndex)
            .Select(x => x.Data)
            .AsAsyncEnumerable()
            .GetAsyncEnumerator(cancellationToken);

        return new ChunkedReadStream(chunkEnumerator);
    }
    /// <summary>
    /// Performs the get string async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        using var stream = await GetStreamAsync(key, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }
    /// <summary>
    /// Performs the get json type async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async Task<JsonRootType?> GetJsonTypeAsync(string key, CancellationToken cancellationToken = default)
    {
        KeyValidation.Validate(key);
        var keyHash = KeyHashing.Compute(key);

        return await dbContext.Jsons
            .AsNoTracking()
            .Where(x => x.KeyHash == keyHash && x.Key == key)
            .Select(x => (JsonRootType?)x.JsonType)
            .SingleOrDefaultAsync(cancellationToken);
    }
    /// <summary>
    /// Performs the get object properties async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async IAsyncEnumerable<JsonElement> GetObjectPropertiesAsync(
        string key,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = await GetTypedStreamAsync(key, JsonRootType.Object, cancellationToken);
        if (stream is null)
        {
            yield break;
        }

        await foreach (var propertyValue in EnumerateFromStreamAsync(stream, JsonRootType.Object, cancellationToken))
        {
            yield return propertyValue;
        }
    }
    /// <summary>
    /// Performs the get array elements async operation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>

    public async IAsyncEnumerable<JsonElement> GetArrayElementsAsync(
        string key,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = await GetTypedStreamAsync(key, JsonRootType.Array, cancellationToken);
        if (stream is null)
        {
            yield break;
        }

        await foreach (var arrayElement in EnumerateFromStreamAsync(stream, JsonRootType.Array, cancellationToken))
        {
            yield return arrayElement;
        }
    }

    private async Task<Stream?> GetTypedStreamAsync(string key, JsonRootType expected, CancellationToken cancellationToken)
    {
        var currentType = await GetJsonTypeAsync(key, cancellationToken);
        if (currentType is null)
        {
            return null;
        }

        if (currentType != expected)
        {
            throw new InvalidOperationException($"Stored JSON is {currentType.Value}, expected {expected}.");
        }

        return await GetStreamAsync(key, cancellationToken);
    }

    private static async IAsyncEnumerable<JsonElement> EnumerateFromStreamAsync(
        Stream jsonStream,
        JsonRootType rootType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[ReadBufferSize];
        var bytesInBuffer = 0;
        var isFinalBlock = false;
        var parsingState = new JsonStreamParserState();

        while (!parsingState.RootCompleted)
        {
            if (!isFinalBlock && bytesInBuffer < buffer.Length)
            {
                var read = await jsonStream.ReadAsync(buffer.AsMemory(bytesInBuffer), cancellationToken);
                if (read == 0)
                {
                    isFinalBlock = true;
                }
                else
                {
                    bytesInBuffer += read;
                }
            }

            var parsedElements = ParseStreamingBatch(
                buffer,
                bytesInBuffer,
                isFinalBlock,
                rootType,
                parsingState,
                out var consumed,
                out var needMoreData);

            if (consumed > 0)
            {
                Buffer.BlockCopy(buffer, consumed, buffer, 0, bytesInBuffer - consumed);
                bytesInBuffer -= consumed;
            }

            foreach (var parsedElement in parsedElements)
            {
                yield return parsedElement;
            }

            if (parsingState.RootCompleted)
            {
                break;
            }

            if (isFinalBlock)
            {
                throw new JsonException("Unexpected end of JSON payload.");
            }

            if (needMoreData && bytesInBuffer == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }
        }
    }

    private static List<JsonElement> ParseStreamingBatch(
        byte[] buffer,
        int bytesInBuffer,
        bool isFinalBlock,
        JsonRootType rootType,
        JsonStreamParserState parsingState,
        out int consumed,
        out bool needMoreData)
    {
        var parsedElements = new List<JsonElement>();
        var reader = new Utf8JsonReader(buffer.AsSpan(0, bytesInBuffer), isFinalBlock, parsingState.ReaderState);
        needMoreData = false;

        while (!parsingState.RootCompleted)
        {
            if (parsingState.AwaitingObjectPropertyValue)
            {
                if (!reader.Read())
                {
                    needMoreData = true;
                    break;
                }

                if (!TryReadCompleteValue(ref reader, out var propertyValue))
                {
                    needMoreData = true;
                    break;
                }

                parsedElements.Add(propertyValue);
                parsingState.AwaitingObjectPropertyValue = false;
                continue;
            }

            if (!reader.Read())
            {
                break;
            }

            if (!parsingState.RootRead)
            {
                var expectedToken = rootType switch
                {
                    JsonRootType.Object => JsonTokenType.StartObject,
                    JsonRootType.Array => JsonTokenType.StartArray,
                    _ => throw new ArgumentOutOfRangeException(nameof(rootType), rootType, "Unsupported root type.")
                };

                if (reader.TokenType != expectedToken)
                {
                    throw new JsonException($"Expected {expectedToken} root token.");
                }

                parsingState.RootRead = true;
                continue;
            }

            if (rootType == JsonRootType.Object)
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 0)
                {
                    parsingState.RootCompleted = true;
                    continue;
                }

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
                {
                    parsingState.AwaitingObjectPropertyValue = true;
                    continue;
                }

                throw new JsonException(
                    $"Unexpected token {reader.TokenType} at depth {reader.CurrentDepth} while reading object properties.");
            }

            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 0)
            {
                parsingState.RootCompleted = true;
                continue;
            }

            if (!TryReadCompleteValue(ref reader, out var arrayElement))
            {
                needMoreData = true;
                break;
            }

            parsedElements.Add(arrayElement);
        }

        consumed = (int)reader.BytesConsumed;
        parsingState.ReaderState = reader.CurrentState;
        return parsedElements;
    }

    private static bool TryReadCompleteValue(ref Utf8JsonReader reader, out JsonElement element)
    {
        var valueReader = reader;
        if (!JsonDocument.TryParseValue(ref valueReader, out var valueDocument))
        {
            element = default;
            return false;
        }

        using (valueDocument)
        {
            element = valueDocument.RootElement.Clone();
        }

        reader = valueReader;
        return true;
    }

    private static void ValidateAndDetectJsonRootType(ReadOnlySpan<byte> chunk, ref JsonRootType? jsonType, ref int bomState)
    {
        if (jsonType is not null)
        {
            return;
        }

        foreach (var currentByte in chunk)
        {
            if (bomState < 3)
            {
                if (bomState == 0 && currentByte == 0xEF)
                {
                    bomState = 1;
                    continue;
                }

                if (bomState == 1 && currentByte == 0xBB)
                {
                    bomState = 2;
                    continue;
                }

                if (bomState == 2 && currentByte == 0xBF)
                {
                    bomState = 3;
                    continue;
                }

                bomState = 3;
            }

            if (currentByte is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                continue;
            }

            jsonType = currentByte switch
            {
                (byte)'{' => JsonRootType.Object,
                (byte)'[' => JsonRootType.Array,
                _ => throw new JsonException(
                    $"Only JSON objects and arrays are supported as root values. Found byte 0x{currentByte:X2}.")
            };

            return;
        }
    }

    private sealed class JsonStreamParserState
    {
        /// <summary>
        /// The current UTF-8 JSON reader state.
        /// </summary>
        public JsonReaderState ReaderState;

        /// <summary>
        /// Indicates whether the root token has been read.
        /// </summary>
        public bool RootRead;

        /// <summary>
        /// Indicates whether the parser is waiting for an object property value.
        /// </summary>
        public bool AwaitingObjectPropertyValue;

        /// <summary>
        /// Indicates whether the root value has been fully parsed.
        /// </summary>
        public bool RootCompleted;
    }
}
