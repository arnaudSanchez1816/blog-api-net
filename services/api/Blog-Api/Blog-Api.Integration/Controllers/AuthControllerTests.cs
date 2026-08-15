using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using BlogApi.Authentication;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Services.Auth;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class AuthControllerTests : IntegrationTestBase
{
    private IAuthService _authService = null!;
    private DataContext _context = null!;
    private LinkGenerator _linkGenerator = null!;
    private ITokensService _tokensService = null!;

    public AuthControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _authService = GetRequiredService<IAuthService>();
        _context = GetRequiredService<DataContext>();
        _tokensService = GetRequiredService<ITokensService>();
        _linkGenerator = GetRequiredService<LinkGenerator>();
    }

    private static (string Name, string Value, CookieOptions Options) ParseCookieString(string cookieString)
    {
        // 1. Parse the raw string into a structured header object
        SetCookieHeaderValue parsedHeader = SetCookieHeaderValue.Parse(cookieString);

        // 2. Map the properties over to a new CookieOptions instance
        CookieOptions options = new CookieOptions
        {
            Domain = parsedHeader.Domain.Value,
            Path = parsedHeader.Path.Value,
            Expires = parsedHeader.Expires,
            MaxAge = parsedHeader.MaxAge,
            Secure = parsedHeader.Secure,
            HttpOnly = parsedHeader.HttpOnly,
            SameSite = (SameSiteMode)(int)parsedHeader.SameSite
        };

        // 3. Return the key-value pair along with the reconstructed options
        return (parsedHeader.Name.Value!, parsedHeader.Value.Value!, options);
    }

    private async Task<(string refreshToken, string accessToken)> RegisterAndLoginAsync(
        string email = "admin@email.com")
    {
        const string username = "admin";
        const string password = "Password1234";
        await _authService.Register(username, email, password);

        LoginRequest request = new LoginRequest
        {
            Email = email,
            Password = password
        };
        HttpResponseMessage loginResponse =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginResponse? loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();

        bool hasCookieHeaders =
            loginResponse.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
        hasCookieHeaders.Should().BeTrue();
        string? refreshTokenCookie =
            cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
        refreshTokenCookie.Should().NotBeEmpty();

        (string _, string value, CookieOptions _) = ParseCookieString(refreshTokenCookie);
        return (value, loginResult.AccessToken);
    }

    #region Login

    [Fact]
    public async Task Login_Returns200_WhenGivenValidCredentials()
    {
        const string username = "admin";
        const string email = "admin@email.com";
        const string password = "Password1234";
        AuthenticationResult registerResult = await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        LoginRequest request = new LoginRequest
        {
            Email = email,
            Password = password
        };
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LoginResponse? loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        loginResponse.Should().NotBeNull();
        loginResponse.AccessToken.Should().NotBeEmpty();
        loginResponse.User.Id.Should().Be(registerResult.User!.Id);
        loginResponse.User.Name.Should().Be(registerResult.User!.DisplayName);
        loginResponse.User.Email.Should().Be(registerResult.User!.Email);
    }

    [Fact]
    public async Task Login_SetRefreshTokenCookie_WhenGivenValidCredentials()
    {
        const string username = "admin";
        const string email = "admin@email.com";
        const string password = "Password1234";
        await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        LoginRequest request = new LoginRequest
        {
            Email = email,
            Password = password
        };
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bool hasCookieHeaders =
            response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
        hasCookieHeaders.Should().BeTrue();

        string? refreshTokenCookie =
            cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
        refreshTokenCookie.Should().NotBeEmpty();

        (string name, string value, CookieOptions options) = ParseCookieString(refreshTokenCookie);
        name.Should().Be(RefreshTokenAuthDefaults.RefreshTokenCookie);
        value.Should().NotBeEmpty();
        options.HttpOnly.Should().BeTrue();
        options.Secure.Should().BeTrue();
        options.MaxAge.Should()
            .BeCloseTo(RefreshTokenAuthDefaults.RefreshTokenCookieMaxAge,
                TimeSpan.FromSeconds(1));
        options.SameSite.Should().Be(SameSiteMode.Strict);
        string? expectedPath = _linkGenerator.GetPathByAction("GetAccessToken", "Auth", new { version = "1" });
        options.Path.Should().Be(expectedPath!);
    }

    [Fact]
    public async Task Login_Returns400_WhenCredentialsAreInvalid()
    {
        const string username = "admin";
        const string password = "Password1234";
        const string email = "admin@email.com";
        await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        LoginRequest request = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword12345"
        };
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        bool hasCookieHeaders =
            response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
        if (hasCookieHeaders)
        {
            string? refreshTokenCookie =
                cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
            refreshTokenCookie.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalidEmail@")]
    [InlineData("invalidEmail@com")]
    [InlineData("invalidEmail")]
    public async Task Login_Returns400_WhenEmailIsInvalid(string? email)
    {
        const string username = "admin";
        const string password = "Password1234";
        await _authService.Register(username, "admin@email.com", password, ct: TestContext.Current.CancellationToken);

        LoginRequest request = new LoginRequest
        {
            Email = email!,
            Password = password
        };
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Returns400_WhenEmailIsAbsent()
    {
        const string username = "admin";
        const string email = "admin@email.com";
        const string password = "Password1234";
        await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login",
                new { password },
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Returns400_WhenPasswordIsEmpty()
    {
        const string username = "admin";
        const string password = "Password1234";
        const string email = "admin@email.com";
        await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        LoginRequest request = new LoginRequest
        {
            Email = email,
            Password = string.Empty
        };
        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Returns400_WhenPasswordIsAbsent()
    {
        const string username = "admin";
        const string password = "Password1234";
        const string email = "admin@email.com";
        await _authService.Register(username, email, password, ct: TestContext.Current.CancellationToken);

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1/auth/login", new { email }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Logout

    private static HttpRequestMessage CreateLogoutTokenRequest(string? refreshTokenCookieValue = null)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/logout");
        if (refreshTokenCookieValue is not null)
        {
            request.Headers.Add("Cookie",
                $"{RefreshTokenAuthDefaults.RefreshTokenCookie}={refreshTokenCookieValue}");
        }

        return request;
    }

    [Fact]
    public async Task Logout_Returns200_WhenCookieIsProvided()
    {
        (string token, string _) = await RegisterAndLoginAsync();

        HttpResponseMessage response =
            await HttpClient.SendAsync(CreateLogoutTokenRequest(token), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Returns200_WhenNoCookieIsProvided()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/auth/logout", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_RevokeRefreshToken_WhenCalled()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        string decodedToken = Uri.UnescapeDataString(refreshTokenCookieValue);

        HttpResponseMessage response =
            await HttpClient.SendAsync(CreateLogoutTokenRequest(refreshTokenCookieValue),
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshToken? refreshToken = await _tokensService.GetRefreshToken(decodedToken, ct: TestContext.Current.CancellationToken);
        refreshToken.Should().NotBeNull();
        refreshToken.Invalidated.Should().BeTrue();
        refreshToken.IsActive(Factory.TimeProvider.GetUtcNow()).Should().BeFalse();
    }

    [Fact]
    public async Task Logout_RemoveRefreshTokenCookie_WhenCalled()
    {
        (string token, string _) = await RegisterAndLoginAsync();

        HttpResponseMessage response =
            await HttpClient.SendAsync(CreateLogoutTokenRequest(token), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bool hasCookieHeaders =
            response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
        hasCookieHeaders.Should().BeTrue();

        string? refreshTokenCookie =
            cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
        refreshTokenCookie.Should().NotBeEmpty();

        (string _, string value, CookieOptions options) = ParseCookieString(refreshTokenCookie);
        value.Should().BeEmpty();
        options.Expires.Should().NotBeNull();
        options.Expires.Value.Should().BeBefore(DateTimeOffset.UtcNow);
    }

    #endregion

    #region GetAccessToken

    private static HttpRequestMessage CreateGetAccessTokenRequest(string? refreshTokenCookieValue = null)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/token");
        if (refreshTokenCookieValue is not null)
        {
            request.Headers.Add("Cookie",
                $"{RefreshTokenAuthDefaults.RefreshTokenCookie}={refreshTokenCookieValue}");
        }

        return request;
    }

    [Fact]
    public async Task GetAccessToken_Returns200_WhenRefreshTokenCookieIsValid()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();

        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetAccessTokenResponse? tokenResponse =
            await response.Content.ReadFromJsonAsync<GetAccessTokenResponse>(TestContext.Current.CancellationToken);
        tokenResponse.Should().NotBeNull();
        tokenResponse.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAccessToken_RotatesRefreshTokenCookie_WhenSuccessful()
    {
        (string originalRefreshTokenCookieValue, string _) = await RegisterAndLoginAsync();

        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(originalRefreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bool hasCookieHeaders =
            response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
        hasCookieHeaders.Should().BeTrue();
        string? refreshTokenCookie =
            cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
        refreshTokenCookie.Should().NotBeEmpty();
        (string _, string newRefreshTokenCookieValue, CookieOptions _) = ParseCookieString(refreshTokenCookie);
        newRefreshTokenCookieValue.Should().NotBeEmpty();
        newRefreshTokenCookieValue.Should().NotBe(originalRefreshTokenCookieValue);
    }

    [Fact]
    public async Task GetAccessToken_Returns200_WhenRefreshTokenUsedWithinGracePeriod()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        HttpResponseMessage firstResponse = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.TimeProvider.Advance(RefreshToken.UsedGracePeriod - TimeSpan.FromMilliseconds(100));
        HttpResponseMessage secondResponse = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenNoCookieIsProvided()
    {
        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenCookieDoesNotMatchAnyToken()
    {
        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest("not-a-real-refresh-token"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenRefreshTokenIsExpired()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        // Change Token expiration date manually (because there is no setter)
        string decodedToken = Uri.UnescapeDataString(refreshTokenCookieValue);
        RefreshToken? token = await _tokensService.GetRefreshToken(decodedToken, ct: TestContext.Current.CancellationToken);
        token.Should().NotBeNull();
        _context.Entry(token).Property(x => x.ExpirationDate).CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenRefreshTokenIsRevoked()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        HttpResponseMessage logoutResponse =
            await HttpClient.SendAsync(CreateLogoutTokenRequest(refreshTokenCookieValue),
                TestContext.Current.CancellationToken);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage response = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenRefreshTokenWasAlreadyUsedPastGracePeriod()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        HttpResponseMessage firstResponse = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.TimeProvider.Advance(RefreshToken.UsedGracePeriod);
        HttpResponseMessage secondResponse = await HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_Returns401_WhenOnlyBearerTokenIsProvided()
    {
        (string _, string jwtBearer) = await RegisterAndLoginAsync();
        HttpRequestMessage getAccessTokenRequest = CreateGetAccessTokenRequest();
        getAccessTokenRequest.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwtBearer);

        HttpResponseMessage response = await HttpClient.SendAsync(
            getAccessTokenRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccessToken_ConvergesOnSameChildToken_WhenCalledConcurrentlyWithSameCookie()
    {
        (string refreshTokenCookieValue, string _) = await RegisterAndLoginAsync();
        string decodedOriginalToken = Uri.UnescapeDataString(refreshTokenCookieValue);

        Task<HttpResponseMessage> firstRequest = HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);
        Task<HttpResponseMessage> secondRequest = HttpClient.SendAsync(
            CreateGetAccessTokenRequest(refreshTokenCookieValue),
            TestContext.Current.CancellationToken);
        HttpResponseMessage[] responses = await Task.WhenAll(firstRequest, secondRequest);

        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

        string ExtractRefreshTokenCookieValue(HttpResponseMessage response)
        {
            bool hasCookieHeaders =
                response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string>? cookieHeaders);
            hasCookieHeaders.Should().BeTrue();
            string? refreshTokenCookie =
                cookieHeaders!.FirstOrDefault(x => x.StartsWith($"{RefreshTokenAuthDefaults.RefreshTokenCookie}="));
            refreshTokenCookie.Should().NotBeEmpty();
            (string _, string value, CookieOptions _) = ParseCookieString(refreshTokenCookie);
            return value;
        }

        string firstChildToken = ExtractRefreshTokenCookieValue(responses[0]);
        string secondChildToken = ExtractRefreshTokenCookieValue(responses[1]);
        firstChildToken.Should().Be(secondChildToken);

        RefreshToken? originalToken = await _tokensService.GetRefreshToken(decodedOriginalToken, ct: TestContext.Current.CancellationToken);
        originalToken.Should().NotBeNull();
        originalToken.ReplacedByToken.Should().NotBeNull();
        originalToken.ReplacedByToken!.Token.Should().Be(Uri.UnescapeDataString(firstChildToken));
    }

    #endregion
}