using System.Security.Claims;
using System.Text.Encodings.Web;
using BlogApi.Domain;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BlogApi.Authentication;

public class RefreshTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokensService _tokensService;

    public RefreshTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder,
        ITokensService tokensService) : base(options, logger, encoder)
    {
        _tokensService = tokensService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenAuthDefaults.RefreshTokenCookie, out string? refreshToken))
        {
            return AuthenticateResult.NoResult();
        }

        RefreshToken? tokenEntity = await _tokensService.GetRefreshToken(refreshToken);
        if (tokenEntity is null || !tokenEntity.IsActive)
        {
            return AuthenticateResult.Fail("Invalid refresh token");
        }

        Context.Items[RefreshTokenAuthDefaults.RefreshTokenHttpContextItem] = tokenEntity;

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, tokenEntity.UserId.ToString()),
            new Claim(ClaimTypes.AuthenticationMethod, RefreshTokenAuthDefaults.RefreshTokenScheme)
        ];
        ClaimsPrincipal principal =
            new ClaimsPrincipal(new ClaimsIdentity(claims, RefreshTokenAuthDefaults.RefreshTokenScheme));

        return AuthenticateResult.Success(new AuthenticationTicket(principal,
            RefreshTokenAuthDefaults.RefreshTokenScheme));
    }
}