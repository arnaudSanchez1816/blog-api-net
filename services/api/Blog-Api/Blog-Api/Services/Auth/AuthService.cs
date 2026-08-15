using System.Security.Claims;
using BlogApi.Domain;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Services.Auth;

public class AuthService : IAuthService
{
    private const string InvalidEmailOrPasswordMessage = "Invalid e-mail or password.";

    private readonly RoleManager<BlogRole> _roleManager;
    private readonly TimeProvider _timeProvider;
    private readonly ITokensService _tokensService;
    private readonly UserManager<BlogUser> _userManager;

    public AuthService(UserManager<BlogUser> userManager, ITokensService tokensService,
        RoleManager<BlogRole> roleManager, TimeProvider timeProvider)
    {
        _userManager = userManager;
        _tokensService = tokensService;
        _roleManager = roleManager;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticationResult> Login(string email, string password, CancellationToken ct = default)
    {
        BlogUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = [InvalidEmailOrPasswordMessage]
            };
        }

        bool validPassword = await _userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = [InvalidEmailOrPasswordMessage]
            };
        }

        RefreshToken refreshToken = await _tokensService.GenerateAndSaveRefreshToken(user, ct);
        return await GenerateAuthenticationResultForUser(user, refreshToken);
    }

    public async Task Logout(RefreshToken token, CancellationToken ct = default)
    {
        await _tokensService.RevokeRefreshToken(token, ct);
    }

    public async Task<AuthenticationResult> RefreshTokens(ClaimsPrincipal principal, RefreshToken refreshToken,
        CancellationToken ct = default)
    {
        BlogUser? user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["User does not exists"]
            };
        }

        if (refreshToken.Used && !refreshToken.IsWithinGracePeriod(_timeProvider.GetUtcNow()))
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["Refresh token was already used."]
            };
        }

        if (refreshToken.IsExpired(_timeProvider.GetUtcNow()))
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["Refresh token is expired."]
            };
        }

        if (refreshToken.Invalidated)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["Refresh token has been revoked."]
            };
        }

        RefreshToken newRefreshToken;
        if (!refreshToken.Used)
        {
            // Use refresh token and pass replacement token reference
            newRefreshToken = _tokensService.CreateRefreshToken(user);
            try
            {
                await _tokensService.UseRefreshToken(refreshToken, newRefreshToken, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Bypass this DbContext's identity map: refreshToken is still tracked here with the
                // in-memory mutations we just made and failed to save, so a tracked re-query would hand
                // those rejected values right back instead of the other request's real committed replacement.
                RefreshToken? refreshedTokenInstance =
                    await _tokensService.GetRefreshToken(refreshToken.Token, true, ct);
                if (refreshedTokenInstance is { ReplacedByToken: not null } &&
                    refreshedTokenInstance.IsWithinGracePeriod(_timeProvider.GetUtcNow()))
                {
                    // Race condition where two calls happen with the same refresh token before it is set to Used
                    // Use the refreshed token replacement token instead of generating a new one
                    return await GenerateAuthenticationResultForUser(user, refreshedTokenInstance.ReplacedByToken);
                }

                return new AuthenticationResult
                {
                    Success = false,
                    Errors = ["Refresh token was already used"]
                };
            }
        }
        else
        {
            newRefreshToken = refreshToken.ReplacedByToken ??
                              throw new InvalidOperationException("Refresh token replacement reference is null");
        }

        return await GenerateAuthenticationResultForUser(user, newRefreshToken);
    }

    public async Task<AuthenticationResult> Register(string displayName, string email, string password,
        IReadOnlyCollection<string>? roles = null, CancellationToken ct = default)
    {
        BlogUser? user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["User with this email already exists."]
            };
        }

        BlogUser newUser = new BlogUser
        {
            UserName = email,
            DisplayName = displayName,
            Email = email
        };

        IdentityResult result = await _userManager.CreateAsync(newUser, password);
        if (!result.Succeeded)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        if (roles is not null)
        {
            await _userManager.AddToRolesAsync(newUser, roles);
        }

        RefreshToken newRefreshToken = await _tokensService.GenerateAndSaveRefreshToken(newUser, ct);
        return await GenerateAuthenticationResultForUser(newUser, newRefreshToken);
    }

    private async Task<AuthenticationResult> GenerateAuthenticationResultForUser(BlogUser user,
        RefreshToken refreshToken)
    {
        IList<Claim> userClaims = await _userManager.GetClaimsAsync(user);
        List<Claim> customClaims = new List<Claim>(userClaims);
        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        foreach (string role in userRoles)
        {
            customClaims.Add(new Claim("roles", role));
            BlogRole? blogRole = await _roleManager.FindByNameAsync(role);
            if (blogRole is not null)
            {
                IList<Claim> roleClaims =
                    await _roleManager.GetClaimsAsync(blogRole);
                customClaims.AddRange(roleClaims);
            }
        }

        string accessToken = _tokensService.GenerateAccessToken(user, customClaims);

        return new AuthenticationResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = user
        };
    }
}