using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonSchemaBuilder.Tests;

public class JsonSchemaBuilderTests
{
    private readonly JsonSchemaBuilder _sut = new();

    [Test]
    public async Task BuildSchema_WithSingleTypeAndVersionProperty_UsesVersionInSchemaId()
    {
        // Arrange
        const string idPrefix = "https://schemas.example.com";

        // Act
        JsonObject schema = _sut.BuildSchema<Models.Invoice>(idPrefix);

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
        JsonObject schema = _sut.BuildSchema(
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
        JsonObject firstSchema = _sut.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix);

        // Act
        JsonObject secondSchema = _sut.BuildSchema(
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
        JsonObject firstSchema = _sut.BuildSchema(
            [typeof(SchemaSet.Alpha.OrderSchema), typeof(SchemaSet.Beta.CustomerSchema)],
            idPrefix);

        // Act
        JsonObject secondSchema = _sut.BuildSchema(
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
