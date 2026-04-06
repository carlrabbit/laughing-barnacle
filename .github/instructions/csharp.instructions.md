---
applyTo: "**/*.cs"
---

# C# Coding Instructions

## Language Version

Use **C# 14** language features targeting **.NET 10**. Embrace modern idioms:

- **Primary constructors** for dependency injection and record-like types.
- **Collection expressions** (`[1, 2, 3]`) instead of `new List<T> { ... }` where possible.
- **Pattern matching** with `is`, `switch`, and property patterns.
- **Raw string literals** (`"""..."""`) for multi-line strings and JSON literals.
- **Required members** (`required`) instead of constructor boilerplate where appropriate.

## Namespaces and Usings

- Use **file-scoped namespaces**: `namespace LaughingBarnacle.Features.WeatherForecast;`
- Place global usings in `Usings.cs` at the project root; avoid per-file `using` directives for common BCL types already in `Usings.cs`.
- Group usings: System namespaces first, then Microsoft, then third-party, then project-internal.

## Nullable Reference Types

- All projects have `<Nullable>enable</Nullable>`.
- Annotate parameters, return types, and properties explicitly (`string?` vs `string`).
- Use the null-forgiving operator (`!`) sparingly and only with a comment explaining why it is safe.

## Async

- Suffix all async methods with `Async` (e.g., `GetWeatherAsync`).
- Return `Task` / `Task<T>` / `ValueTask<T>` as appropriate.
- Use `CancellationToken` parameters for all async public APIs, forwarding them downstream.

## Error Handling

- Use **typed exceptions** specific to the domain; avoid catching `Exception` broadly.
- Log exceptions with `ILogger` before re-throwing or returning error responses.
- In API layer, map domain exceptions to appropriate HTTP status codes via exception middleware.

## Records and DTOs

- Use `record` or `record struct` for immutable data transfer objects.
- Use `class` for mutable domain entities.

## Formatting

- Use **4-space indentation** (enforced by `.editorconfig`).
- Opening braces on the **same line** as the declaration (K&R / "Egyptian braces" style, not Allman style).
- Maximum line length: 120 characters.
