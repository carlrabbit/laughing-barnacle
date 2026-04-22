using System.Text;
using System.Text.Json;
using Jsoncli;
using Jsonstore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jsonstore.Tests;

/// <summary>
/// Documentation.
/// </summary>
public class JsonStoreBehaviorTests
{
    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task StoreAsync_WithLargeJsonStream_SplitsIntoChunksAndReadsBack()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var payload = BuildLargeJson(300_000);
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(payload), writable: false);

        // Act
        var saved = await store.StoreAsync("json-key", source);
        using var retrieved = await store.GetStreamAsync("json-key");
        using var copied = new MemoryStream();
        await retrieved!.CopyToAsync(copied);

        // Assert
        await Assert.That(saved.Created).IsTrue();
        await Assert.That(saved.ChunkCount > 1).IsTrue();
        await Assert.That(saved.JsonType).IsEqualTo(JsonRootType.Object);
        await Assert.That(Encoding.UTF8.GetString(copied.ToArray())).IsEqualTo(payload);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task StoreAsync_WithPrimitiveJson_ThrowsJsonException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var primitiveJsonValues = new[] { "123", "\"text\"", "true", "false", "null" };

        // Act
        var caughtExceptions = new List<JsonException>();
        for (var i = 0; i < primitiveJsonValues.Length; i++)
        {
            var primitiveJson = primitiveJsonValues[i];
            try
            {
                await store.StoreAsync($"primitive-key-{i}", primitiveJson);
            }
            catch (JsonException exception)
            {
                caughtExceptions.Add(exception);
            }
        }

        // Assert
        await Assert.That(caughtExceptions.Count).IsEqualTo(primitiveJsonValues.Length);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task StoreAsync_WhenCalledTwiceOnSameKey_DoesNotOverwrite()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);

        // Act
        var first = await store.StoreAsync("my-key", "{\"v\":1}");
        var second = await store.StoreAsync("my-key", "{\"v\":2}");
        var stored = await store.GetStringAsync("my-key");

        // Assert
        await Assert.That(first.Created).IsTrue();
        await Assert.That(second.Created).IsFalse();
        await Assert.That(first.JsonType).IsEqualTo(JsonRootType.Object);
        await Assert.That(second.JsonType).IsEqualTo(JsonRootType.Object);
        await Assert.That(stored).IsEqualTo("{\"v\":1}");
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetObjectPropertiesAsync_WithObjectValue_ReturnsPropertyValues()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        await store.StoreAsync("object-key", "{\"a\":1,\"b\":[2,3],\"c\":{\"ok\":true}}");
        var values = new List<string>();

        // Act
        await foreach (var propertyValue in store.GetObjectPropertiesAsync("object-key"))
        {
            values.Add(propertyValue.GetRawText());
        }

        // Assert
        await Assert.That(values).IsEquivalentTo(["1", "[2,3]", "{\"ok\":true}"]);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetObjectPropertiesAsync_WithComplexStringProperties_ReturnsExactStringValues()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var embeddedJsonText = """{"nested":[1,2,{"k":"v"}]}""";
        var markdownText = """
# Edge case title

- bullet 1
- bullet 2

```json
{"a":1}
```
""";
        var payload = JsonSerializer.Serialize(new
        {
            embeddedJson = embeddedJsonText,
            markdown = markdownText
        });
        await store.StoreAsync("complex-object-key", payload);
        var values = new List<JsonElement>();

        // Act
        await foreach (var propertyValue in store.GetObjectPropertiesAsync("complex-object-key"))
        {
            values.Add(propertyValue);
        }

        // Assert
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[1].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[0].GetString()).IsEqualTo(embeddedJsonText);
        await Assert.That(values[1].GetString()).IsEqualTo(markdownText);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetObjectPropertiesAsync_WithMalformedJsonTextAndUtf8Strings_ReturnsExactStringValues()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var malformedJsonText = """{"broken":[1,2,} "unterminated": true""";
        var utf8Text = "Emoji 😀 · Greek Ω · Japanese 日本語 · Arabic العربية · Accents café naïve";
        var payload = JsonSerializer.Serialize(new
        {
            malformed = malformedJsonText,
            utf8 = utf8Text
        });
        await store.StoreAsync("malformed-and-utf8-object-key", payload);
        var values = new List<JsonElement>();

        // Act
        await foreach (var propertyValue in store.GetObjectPropertiesAsync("malformed-and-utf8-object-key"))
        {
            values.Add(propertyValue);
        }

        // Assert
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[1].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[0].GetString()).IsEqualTo(malformedJsonText);
        await Assert.That(values[1].GetString()).IsEqualTo(utf8Text);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetArrayElementsAsync_WithArrayValue_ReturnsElements()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        await store.StoreAsync("array-key", "[{\"id\":1},{\"id\":2},3]");
        var elements = new List<string>();

        // Act
        await foreach (var element in store.GetArrayElementsAsync("array-key"))
        {
            elements.Add(element.GetRawText());
        }

        // Assert
        await Assert.That(elements).IsEquivalentTo(["{\"id\":1}", "{\"id\":2}", "3"]);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetArrayElementsAsync_WithComplexStringElements_ReturnsExactStringValues()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var embeddedJsonText = """{"outer":{"inner":[true,false]}}""";
        var markdownText = """
## Markdown sample

> quoted line

| A | B |
| - | - |
| 1 | 2 |
""";
        var payload = JsonSerializer.Serialize(new[]
        {
            embeddedJsonText,
            markdownText
        });
        await store.StoreAsync("complex-array-key", payload);
        var values = new List<JsonElement>();

        // Act
        await foreach (var element in store.GetArrayElementsAsync("complex-array-key"))
        {
            values.Add(element);
        }

        // Assert
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[1].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[0].GetString()).IsEqualTo(embeddedJsonText);
        await Assert.That(values[1].GetString()).IsEqualTo(markdownText);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task GetArrayElementsAsync_WithMalformedJsonTextAndUtf8Strings_ReturnsExactStringValues()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new WriteOnceJsonStore(dbContext);
        var malformedJsonText = """{"bad":"json", "oops": [1,2,3,,]}""";
        var utf8Text = "Snowman ☃️ · Math ∑ · Cyrillic Привет · Chinese 汉字 · Currency €";
        var payload = JsonSerializer.Serialize(new[]
        {
            malformedJsonText,
            utf8Text
        });
        await store.StoreAsync("malformed-and-utf8-array-key", payload);
        var values = new List<JsonElement>();

        // Act
        await foreach (var element in store.GetArrayElementsAsync("malformed-and-utf8-array-key"))
        {
            values.Add(element);
        }

        // Assert
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[1].ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(values[0].GetString()).IsEqualTo(malformedJsonText);
        await Assert.That(values[1].GetString()).IsEqualTo(utf8Text);
    }

    /// <summary>
    /// Documentation.
    /// </summary>
    [Test]
    public async Task CreateClient_WithDependencyInjection_StoresAndFetchesValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddJsonStore(options => options.UseInMemoryDatabase($"jsonstore-{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IJsonClientFactory>();
        var client = factory.CreateClient();

        // Act
        var created = await client.StoreAsync("di-key", "{\"ok\":true}");
        var fetched = await client.GetStringAsync("di-key");
        var jsonType = await client.GetJsonTypeAsync("di-key");
        var values = new List<string>();
        await foreach (var propertyValue in client.GetObjectPropertiesAsync("di-key"))
        {
            values.Add(propertyValue.GetRawText());
        }

        // Assert
        await Assert.That(created.Created).IsTrue();
        await Assert.That(created.JsonType).IsEqualTo(JsonRootType.Object);
        await Assert.That(fetched).IsEqualTo("{\"ok\":true}");
        await Assert.That(jsonType).IsEqualTo(JsonRootType.Object);
        await Assert.That(values).IsEquivalentTo(["true"]);
    }

    private static JsonStoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JsonStoreDbContext>()
            .UseInMemoryDatabase($"jsonstore-tests-{Guid.NewGuid():N}")
            .Options;

        return new JsonStoreDbContext(options);
    }

    private static string BuildLargeJson(int valueCount)
    {
        var numbers = string.Join(',', Enumerable.Range(0, valueCount));
        return $"{{\"items\":[{numbers}]}}";
    }
}
