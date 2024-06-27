using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApiOAuth.Tests;

public class WebApiTests
{
    private WebApplicationFactory<Program> factory;

    private static X509Certificate2 CreateCertificate()
    {
        var rsa = RSA.Create();
        var certificateRequest = new CertificateRequest(
            "CN=WebApiOAuth",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        var certificate = certificateRequest.CreateSelfSigned(
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddDays(1)
        );
        return certificate;
    }

    [SetUp]
    public void Setup()
    {
        factory = new WebApplicationFactory<Program>();
    }

    [Test]
    public async Task UnauthorizedHttpCallFails()
    {
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync("/WeatherForecast");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthorizedHttpCallSucceeds()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "web-api-oauth-test")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            )
            .Encode();

        using var httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var weatherForecasts = await httpClient.GetFromJsonAsync<IEnumerable<WeatherForecast>>(
            "/WeatherForecast"
        );

        weatherForecasts.Should().HaveCount(5);
    }

    [Test]
    public async Task AuthorizedHttpCallWithExpiredTokenFails()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "web-api-oauth-test")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            )
            .Encode();

        using var httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await httpClient.GetAsync("/WeatherForecast");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthorizedHttpCallWithInvalidAudienceFails()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "invalid")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            )
            .Encode();

        using var httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await httpClient.GetAsync("/WeatherForecast");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TearDown]
    public void TearDown()
    {
        factory.Dispose();
    }
}
