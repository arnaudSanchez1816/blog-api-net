using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity.Data;

namespace BlogApi.Integration.RateLimit;

public class RateLimitingTests : IAsyncLifetime
{
    private readonly RateLimitedBlogApiFactory _factory = new RateLimitedBlogApiFactory();

    private HttpClient HttpClient
    {
        get => _factory.HttpClient;
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_Returns429WithRetryAfterHeader_WhenAuthRateLimitIsExceeded()
    {
        // Arrange
        _factory.SetAuthRateLimit(2);
        LoginRequest request = new LoginRequest
        {
            Email = "rate-limit-test@example.com",
            Password = "wrong-password"
        };

        // Act
        HttpResponseMessage first = await HttpClient.PostAsJsonAsync("api/v1/auth/login",
            request,
            TestContext.Current.CancellationToken);
        HttpResponseMessage second = await HttpClient.PostAsJsonAsync("api/v1/auth/login",
            request,
            TestContext.Current.CancellationToken);
        HttpResponseMessage third = await HttpClient.PostAsJsonAsync("api/v1/auth/login",
            request,
            TestContext.Current.CancellationToken);

        // Assert: the auth policy allows 2 requests per window before rejecting.
        first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        third.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTags_Returns429_WhenGlobalRateLimitIsExceeded()
    {
        //Arrange
        _factory.SetGlobalRateLimit(3);

        // Act
        HttpResponseMessage first = await HttpClient.GetAsync("api/v1/tags", TestContext.Current.CancellationToken);
        HttpResponseMessage second = await HttpClient.GetAsync("api/v1/tags", TestContext.Current.CancellationToken);
        HttpResponseMessage third = await HttpClient.GetAsync("api/v1/tags", TestContext.Current.CancellationToken);
        HttpResponseMessage fourth = await HttpClient.GetAsync("api/v1/tags", TestContext.Current.CancellationToken);

        // Assert: the global policy allows 3 requests per window before rejecting.
        first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        third.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        fourth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}