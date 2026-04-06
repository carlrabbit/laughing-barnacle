---
applyTo: "tests/**/*.cs"
---

# Test Coding Instructions

## Framework and Libraries

- Use **xUnit** for all tests.
- Use **Moq** (or **NSubstitute**) for mocking.
- Use **FluentAssertions** for expressive assertions (`result.Should().Be(...)`).
- Use **Microsoft.AspNetCore.Mvc.Testing** (`WebApplicationFactory<Program>`) for integration tests.

## Test Structure

Follow the **Arrange / Act / Assert (AAA)** pattern with a blank line separating each section:

```csharp
[Fact]
public async Task GetWeatherForecast_WhenCalled_ReturnsOkWithForecasts()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/weatherforecast");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Naming Conventions

Name test methods using: `MethodName_StateUnderTest_ExpectedBehavior`

Examples:
- `CreateOrder_WithValidInput_ReturnsCreatedResult`
- `GetById_WhenNotFound_ReturnsNotFound`
- `ProcessPayment_WhenInsufficientFunds_ThrowsInvalidOperationException`

## Test Classes

- One test class per production class or feature.
- Use `IClassFixture<T>` or `ICollectionFixture<T>` for expensive shared state (e.g., `WebApplicationFactory`).
- Mark slow or integration tests with `[Trait("Category", "Integration")]` so they can be filtered.

## What to Test

- Test **behaviour**, not implementation details.
- Cover the **happy path** and meaningful **edge cases** (null inputs, empty collections, boundary values).
- Do **not** test framework code (e.g., built-in serialization) unless your code wraps it with custom logic.

## Mocking

- Mock only **external dependencies** (database, HTTP clients, clocks).
- Prefer injecting interfaces so dependencies can be easily mocked.
- Verify mock expectations explicitly when the interaction is important to the test.
