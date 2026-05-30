using System.Text.Json;
using Bincli;
using Binstore;
using Jsoncli;
using Jsonstore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddBinStore(options =>
    options.UseInMemoryDatabase(builder.Configuration.GetValue("BinStore:DatabaseName", "binstore-api")));
builder.Services.AddJsonStore(options =>
    options.UseInMemoryDatabase(builder.Configuration.GetValue("JsonStore:DatabaseName", "jsonstore-api")));

var app = builder.Build();

app.MapOpenApi();

app.MapPost("/bin/{key}", async Task<IResult> (
    string key,
    HttpContext httpContext,
    IWriteOnceBinaryStore binStore,
    CancellationToken cancellationToken) =>
{
    var result = await binStore.StoreAsync(key, httpContext.Request.Body, cancellationToken);
    return result.Created
        ? Results.Created($"/bin/{Uri.EscapeDataString(key)}", result)
        : Results.Conflict(result);
})
.Accepts<byte[]>("application/octet-stream")
.Produces<BinStoreResult>(StatusCodes.Status201Created)
.Produces<BinStoreResult>(StatusCodes.Status409Conflict)
.ProducesValidationProblem()
.WithName("StoreBinary")
.WithSummary("Stores binary data for a key.");

app.MapGet("/bin/{key}", async Task<IResult> (
    string key,
    IWriteOnceBinaryStore binStore,
    CancellationToken cancellationToken) =>
{
    var stream = await binStore.GetStreamAsync(key, cancellationToken);
    if (stream is null)
    {
        return Results.NotFound();
    }

    return Results.Stream(stream, contentType: "application/octet-stream");
})
.Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
.Produces(StatusCodes.Status404NotFound)
.WithName("GetBinary")
.WithSummary("Streams stored binary data for a key.");

app.MapPost("/json/{key}", async Task<IResult> (
    string key,
    HttpContext httpContext,
    IWriteOnceJsonStore jsonStore,
    CancellationToken cancellationToken) =>
{
    var result = await jsonStore.StoreAsync(key, httpContext.Request.Body, cancellationToken);
    return result.Created
        ? Results.Created($"/json/{Uri.EscapeDataString(key)}", result)
        : Results.Conflict(result);
})
.Accepts<object>("application/json")
.Produces<JsonStoreResult>(StatusCodes.Status201Created)
.Produces<JsonStoreResult>(StatusCodes.Status409Conflict)
.ProducesValidationProblem()
.WithName("StoreJson")
.WithSummary("Stores JSON object or array data for a key.");

app.MapGet("/json/{key}", async Task<IResult> (
    string key,
    IWriteOnceJsonStore jsonStore,
    CancellationToken cancellationToken) =>
{
    var stream = await jsonStore.GetStreamAsync(key, cancellationToken);
    if (stream is null)
    {
        return Results.NotFound();
    }

    return Results.Stream(stream, contentType: "application/json");
})
.Produces(StatusCodes.Status200OK, contentType: "application/json")
.Produces(StatusCodes.Status404NotFound)
.WithName("GetJson")
.WithSummary("Streams stored JSON data for a key.");

app.MapGet("/json/{key}/type", async Task<IResult> (
    string key,
    IWriteOnceJsonStore jsonStore,
    CancellationToken cancellationToken) =>
{
    var jsonType = await jsonStore.GetJsonTypeAsync(key, cancellationToken);
    return jsonType is null ? Results.NotFound() : Results.Ok(jsonType.Value);
})
.Produces<JsonRootType>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithName("GetJsonType")
.WithSummary("Gets the stored root JSON type for a key.");

app.MapGet("/json/{key}/object-properties-stream", async Task<IResult> (
    string key,
    HttpContext httpContext,
    IWriteOnceJsonStore jsonStore,
    CancellationToken cancellationToken) =>
{
    var jsonType = await jsonStore.GetJsonTypeAsync(key, cancellationToken);
    if (jsonType is null)
    {
        return Results.NotFound();
    }

    if (jsonType != JsonRootType.Object)
    {
        return Results.Problem(
            detail: "Stored JSON root type is not an object.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    httpContext.Response.ContentType = "application/x-ndjson";

    await foreach (var propertyValue in jsonStore.GetObjectPropertiesAsync(key, cancellationToken)
                       .WithCancellation(cancellationToken))
    {
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, propertyValue, cancellationToken: cancellationToken);
        await httpContext.Response.WriteAsync("\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    return Results.Empty;
})
.Produces(StatusCodes.Status200OK, contentType: "application/x-ndjson")
.ProducesProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.WithName("StreamObjectProperties")
.WithSummary("Streams JSON object property values as NDJSON.");

app.MapGet("/json/{key}/array-elements-stream", async Task<IResult> (
    string key,
    HttpContext httpContext,
    IWriteOnceJsonStore jsonStore,
    CancellationToken cancellationToken) =>
{
    var jsonType = await jsonStore.GetJsonTypeAsync(key, cancellationToken);
    if (jsonType is null)
    {
        return Results.NotFound();
    }

    if (jsonType != JsonRootType.Array)
    {
        return Results.Problem(
            detail: "Stored JSON root type is not an array.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    httpContext.Response.ContentType = "application/x-ndjson";

    await foreach (var arrayElement in jsonStore.GetArrayElementsAsync(key, cancellationToken)
                       .WithCancellation(cancellationToken))
    {
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, arrayElement, cancellationToken: cancellationToken);
        await httpContext.Response.WriteAsync("\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    return Results.Empty;
})
.Produces(StatusCodes.Status200OK, contentType: "application/x-ndjson")
.ProducesProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.WithName("StreamArrayElements")
.WithSummary("Streams JSON array elements as NDJSON.");

app.Run();

/// <summary>
/// Represents the API entry point used by integration tests.
/// </summary>
public partial class Program;
