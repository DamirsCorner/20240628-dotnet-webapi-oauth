using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApiOAuth.Tests;

public class WebApiTests
{
    private WebApplicationFactory<Program> factory;

    [SetUp]
    public void Setup()
    {
        factory = new WebApplicationFactory<Program>();
    }

    [Test]
    public async Task UnauthorizedHttpCallSucceeds()
    {
        using var httpClient = factory.CreateClient();

        var weatherForecasts = await httpClient.GetFromJsonAsync<IEnumerable<WeatherForecast>>(
            "/WeatherForecast"
        );

        weatherForecasts.Should().HaveCount(5);
    }

    [TearDown]
    public void TearDown()
    {
        factory.Dispose();
    }
}
