namespace JsonSchemaBuilder.Tests.SchemaSet.Alpha;

public record OrderSchema
{
    public string Version { get; init; } = "3";

    public string OrderNumber { get; init; } = string.Empty;
}
