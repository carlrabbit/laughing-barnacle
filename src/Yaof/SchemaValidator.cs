using System.Reflection;
using System.Text.Json;
using Json.Schema;

namespace Yaof;

/// <summary>Validates replacement JSON documents against the embedded YAOF JSON Schema.</summary>
public sealed class SchemaValidator
{
    private static readonly JsonSchema EmbeddedSchema = LoadEmbeddedSchema();

    private static JsonSchema LoadEmbeddedSchema()
    {
        string resourceName = "Yaof.Schema.replacement-schema.json";
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        return JsonSchema.FromText(json);
    }

    /// <summary>
    /// Validates the given JSON string against the YAOF replacement schema.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> with validation details.</returns>
    public SchemaValidationResult Validate(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return SchemaValidationResult.Failure([$"Invalid JSON: {ex.Message}"]);
        }

        using (document)
        {
            EvaluationResults results = EmbeddedSchema.Evaluate(
                document.RootElement,
                new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (results.IsValid)
                return SchemaValidationResult.Success();

            List<string> errors = [];
            if (results.Details is not null)
            {
                errors = results.Details
                    .Where(d => !d.IsValid && d.Errors is not null)
                    .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))
                    .ToList();
            }

            return SchemaValidationResult.Failure(errors);
        }
    }
}

/// <summary>The result of a JSON Schema validation.</summary>
public record SchemaValidationResult
{
    /// <summary>
    /// Gets a value indicating whether schema validation succeeded.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful <see cref="SchemaValidationResult"/>.</returns>
    public static SchemaValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed <see cref="SchemaValidationResult"/>.</returns>
    public static SchemaValidationResult Failure(IReadOnlyList<string> errors) =>
        new() { IsValid = false, Errors = errors };
}
