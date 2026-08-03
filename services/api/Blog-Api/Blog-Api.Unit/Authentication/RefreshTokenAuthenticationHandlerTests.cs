using System.Security.Claims;
using System.Text.Encodings.Web;
using AwesomeAssertions;
using BlogApi.Authentication;
using BlogApi.Domain;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BlogApi.Unit.Authentication;

public class RefreshTokenAuthenticationHandlerTests : IDisposable
{
    private readonly Mock<ITokensService> _tokensService;

    public RefreshTokenAuthenticationHandlerTests()
    {
        _tokensService = new Mock<ITokensService>();
    }

    public void Dispose()
    {
        _tokensService.Reset();
    }

    private async Task<(RefreshTokenAuthenticationHandler Handler, DefaultHttpContext Context)> CreateHandler(
        string? refreshTokenCookie = null)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        if (refreshTokenCookie is not null)
        {
            context.Request.Headers.Append("Cookie",
                $"{RefreshTokenAuthDefaults.RefreshTokenCookie}={refreshTokenCookie}");
        }

        Mock<IOptionsMonitor<AuthenticationSchemeOptions>> optionsMonitor =
            new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(x => x.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        RefreshTokenAuthenticationHandler handler = new RefreshTokenAuthenticationHandler(optionsMonitor.Object,
            NullLoggerFactory.Instance, UrlEncoder.Default, _tokensService.Object);

        AuthenticationScheme scheme = new AuthenticationScheme(RefreshTokenAuthDefaults.RefreshTokenScheme, null,
            typeof(RefreshTokenAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);

        return (handler, context);
    }

    private static RefreshToken MakeToken(Guid userId, bool used = false, bool invalidated = false,
        DateTimeOffset? expirationDate = null)
    {
        return new RefreshToken
        {
            Token = "refresh-token-value",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate ?? DateTimeOffset.UtcNow.AddDays(1),
            Used = used,
            Invalidated = invalidated,
            UserId = userId
        };
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsNoResult_WhenCookieIsMissing()
    {
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler();

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.None.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
        _tokensService.Verify(x => x.GetRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Fails_WhenTokenDoesNotExist()
    {
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler("refresh-token-value");
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value"))
            .ReturnsAsync((RefreshToken?)null);

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Fails_WhenTokenIsExpired()
    {
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler("refresh-token-value");
        RefreshToken expiredToken =
            MakeToken(Guid.NewGuid(), expirationDate: DateTimeOffset.UtcNow.AddMinutes(-1));
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value")).ReturnsAsync(expiredToken);

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Fails_WhenTokenIsUsed()
    {
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler("refresh-token-value");
        RefreshToken usedToken = MakeToken(Guid.NewGuid(), used: true);
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value")).ReturnsAsync(usedToken);

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Fails_WhenTokenIsInvalidated()
    {
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler("refresh-token-value");
        RefreshToken invalidatedToken = MakeToken(Guid.NewGuid(), invalidated: true);
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value")).ReturnsAsync(invalidatedToken);

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_Succeeds_WhenTokenIsActive()
    {
        Guid userId = Guid.NewGuid();
        (RefreshTokenAuthenticationHandler handler, _) = await CreateHandler("refresh-token-value");
        RefreshToken activeToken = MakeToken(userId);
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value")).ReturnsAsync(activeToken);

        AuthenticateResult result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket!.AuthenticationScheme.Should().Be(RefreshTokenAuthDefaults.RefreshTokenScheme);
        result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(userId.ToString());
        result.Principal!.FindFirstValue(ClaimTypes.AuthenticationMethod)
            .Should().Be(RefreshTokenAuthDefaults.RefreshTokenScheme);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_StoresRefreshTokenInHttpContextItems_WhenTokenIsActive()
    {
        (RefreshTokenAuthenticationHandler handler, DefaultHttpContext context) =
            await CreateHandler("refresh-token-value");
        RefreshToken activeToken = MakeToken(Guid.NewGuid());
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value")).ReturnsAsync(activeToken);

        await handler.AuthenticateAsync();

        context.Items[RefreshTokenAuthDefaults.RefreshTokenHttpContextItem].Should().Be(activeToken);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_DoesNotStoreRefreshTokenInHttpContextItems_WhenTokenIsInvalid()
    {
        (RefreshTokenAuthenticationHandler handler, DefaultHttpContext context) =
            await CreateHandler("refresh-token-value");
        _tokensService.Setup(x => x.GetRefreshToken("refresh-token-value"))
            .ReturnsAsync((RefreshToken?)null);

        await handler.AuthenticateAsync();

        context.Items.Should().NotContainKey(RefreshTokenAuthDefaults.RefreshTokenHttpContextItem);
    }
}
