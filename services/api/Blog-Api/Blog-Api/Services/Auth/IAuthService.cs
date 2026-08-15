using System.Security.Claims;
using BlogApi.Domain;

namespace BlogApi.Services.Auth;

public interface IAuthService
{
    public Task<AuthenticationResult> Login(string email, string password, CancellationToken ct = default);

    public Task Logout(RefreshToken refreshToken, CancellationToken ct = default);

    public Task<AuthenticationResult> Register(string displayName, string email, string password,
        IReadOnlyCollection<string>? roles = null, CancellationToken ct = default);

    public Task<AuthenticationResult> RefreshTokens(ClaimsPrincipal principal, RefreshToken refreshToken,
        CancellationToken ct = default);
}