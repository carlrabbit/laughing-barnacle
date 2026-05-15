namespace JsonSchemaBuilder.Tests.Models;

public record Invoice
{
    public string Version { get; init; } = "2";

    public string Number { get; init; } = string.Empty;

    public IReadOnlyList<InvoiceLineItem> LineItems { get; init; } = [];
}

public record InvoiceLineItem
{
    public string Name { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
