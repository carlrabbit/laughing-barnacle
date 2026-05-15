using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JsonSchemaBuilder.Tests;

public class JsonSchemaBuilderTests
{
    private readonly JsonSchemaBuilder _schemaBuilder = new();

    [Test]
    public async Task BuildSchema_WithSingleTypeAndVersionProperty_UsesVersionInSchemaId()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";

        // Act
        JsonObject schema = _schemaBuilder.BuildSchema<Models.Invoice>(idPrefix);

        // Assert
        await Assert.That(schema["$schema"]!.GetValue<string>()).IsEqualTo("https://json-schema.org/draft/2020-12/schema");
        await Assert.That(schema["$id"]!.GetValue<string>()).IsEqualTo(
            "https://schemas.example.com/JsonSchemaBuilder/Tests/Models/Invoice/v2");
        await Assert.That(schema["properties"]!["LineItems"]!["type"]!.GetValue<string>()).IsEqualTo("array");
    }

    [Test]
    public async Task BuildSchema_WithMultipleNamespaces_UsesCommonNamespaceForRootId()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";

        // Act
        JsonObject schema = _schemaBuilder.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix);

        // Assert
        await Assert.That(schema["$id"]!.GetValue<string>()).IsEqualTo(
            "https://schemas.example.com/JsonSchemaBuilder/Tests/SchemaSet/v1");

        Dictionary<string, string>? versions = ParseSubSchemaVersions(schema["$comment"]!.GetValue<string>());
        await Assert.That(versions).IsNotNull();
        await Assert.That(versions!).ContainsKey("JsonSchemaBuilder.Tests.SchemaSet.Alpha.OrderSchema");
        await Assert.That(versions).ContainsKey("JsonSchemaBuilder.Tests.SchemaSet.Beta.CustomerSchema");
    }

    [Test]
    public async Task BuildSchema_WithUnchangedSubSchemas_KeepsRootVersion()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";
        JsonObject firstSchema = _schemaBuilder.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix);

        // Act
        JsonObject secondSchema = _schemaBuilder.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix,
            firstSchema);

        // Assert
        await Assert.That(secondSchema["$id"]!.GetValue<string>()).IsEqualTo(firstSchema["$id"]!.GetValue<string>());
    }

    [Test]
    public async Task BuildSchema_WithAddedSubSchema_IncrementsRootVersion()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";
        JsonObject firstSchema = _schemaBuilder.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix);

        // Act
        JsonObject secondSchema = _schemaBuilder.BuildSchema(
            [
                typeof(SchemaSet.Alpha.OrderSchema),
                typeof(SchemaSet.Beta.CustomerSchema),
                typeof(SchemaSet.Gamma.PartnerSchema)
            ],
            idPrefix,
            firstSchema);

        // Assert
        await Assert.That(secondSchema["$id"]!.GetValue<string>()).EndsWith("/v2");
    }

    [Test]
    public async Task BuildSchema_WithSystemTextJsonNamingPolicies_UsesSerializedNames()
    {
        // Arrange
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.KebabCaseLower
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        // Act
        JsonObject schema = _schemaBuilder.BuildSchema<SystemTextJsonModels.AnnotatedEnvelope>(
            "https://schemas.example.com",
            serializerOptions: serializerOptions);

        // Assert
        JsonObject properties = (JsonObject)schema["properties"]!;
        await Assert.That(properties.ContainsKey("identifier")).IsTrue();
        await Assert.That(properties.ContainsKey("status")).IsTrue();
        await Assert.That(properties.ContainsKey("channel_counts")).IsTrue();

        JsonObject statusSchema = (JsonObject)properties["status"]!;
        JsonArray statusValues = (JsonArray)statusSchema["enum"]!;
        await Assert.That(statusSchema["type"]!.GetValue<string>()).IsEqualTo("string");
        await Assert.That(statusValues.Select(static value => value!.GetValue<string>()).ToArray())
            .IsEquivalentTo(["pending_review", "sent_to_customer"]);

        JsonObject channelCountsSchema = (JsonObject)properties["channel_counts"]!;
        JsonObject propertyNamesSchema = (JsonObject)channelCountsSchema["propertyNames"]!;
        JsonArray channelNames = (JsonArray)propertyNamesSchema["enum"]!;
        await Assert.That(channelNames.Select(static value => value!.GetValue<string>()).ToArray())
            .IsEquivalentTo(["email-channel", "sms-channel"]);
    }

    [Test]
    public async Task BuildSchema_WithJsonPolymorphicAnnotations_UsesDerivedSchemasAndDiscriminator()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act
        JsonObject schema = _schemaBuilder.BuildSchema<SystemTextJsonModels.Animal>(
            idPrefix,
            serializerOptions: serializerOptions);

        // Assert
        JsonArray variants = (JsonArray)schema["oneOf"]!;
        await Assert.That(variants.Count).IsEqualTo(2);

        JsonObject catSchema = variants
            .Select(static variant => (JsonObject)variant!)
            .Single(static variant => variant["properties"]!["kind"]!["const"]!.GetValue<string>() == "cat");
        JsonObject dogSchema = variants
            .Select(static variant => (JsonObject)variant!)
            .Single(static variant => variant["properties"]!["kind"]!["const"]!.GetValue<string>() == "dog");
        JsonObject catProperties = (JsonObject)catSchema["properties"]!;
        JsonObject dogProperties = (JsonObject)dogSchema["properties"]!;

        await Assert.That(catProperties["lives"]!["type"]!.GetValue<string>()).IsEqualTo("integer");
        await Assert.That(dogProperties["good_dog"]!["type"]!.GetValue<string>()).IsEqualTo("boolean");
        await Assert.That(catProperties.ContainsKey("ignored_kind")).IsFalse();
        await Assert.That(dogProperties.ContainsKey("ignored_kind")).IsFalse();
        await Assert.That(catSchema["required"]!.AsArray().Select(static value => value!.GetValue<string>()).ToArray())
            .Contains("kind");
        await Assert.That(dogSchema["required"]!.AsArray().Select(static value => value!.GetValue<string>()).ToArray())
            .Contains("kind");
    }

    private static Dictionary<string, string>? ParseSubSchemaVersions(string comment)
    {
        const string prefix = "subschema-versions:";
        if (!comment.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(comment[prefix.Length..]);
    }
}
