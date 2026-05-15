namespace JsonSchemaBuilder.Tests.SchemaSet.Beta;

public record CustomerSchema
{
    public string Version { get; init; } = "5";

    public string Email { get; init; } = string.Empty;
}
