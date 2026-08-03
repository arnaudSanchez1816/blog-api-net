using System.Security.Claims;
using BlogApi.Domain;

namespace BlogApi.Services.Auth;

public interface IAuthService
{
    public Task<AuthenticationResult> Login(string email, string password);

    public Task Logout(RefreshToken refreshToken);

    public Task<AuthenticationResult> Register(string username, string email, string password);

    public Task<AuthenticationResult> RefreshTokens(ClaimsPrincipal principal, RefreshToken refreshToken);
}