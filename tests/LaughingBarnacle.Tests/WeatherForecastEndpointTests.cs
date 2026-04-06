using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaughingBarnacle.Tests;

public class WeatherForecastEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetWeatherForecast_WhenCalled_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_WhenCalled_ReturnsFiveForecasts()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");
        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecastDto[]>();

        // Assert
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
    }

    private sealed record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
