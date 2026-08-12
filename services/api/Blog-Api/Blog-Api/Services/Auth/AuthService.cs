using System.Security.Claims;
using BlogApi.Domain;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Services.Auth;

public class AuthService : IAuthService
{
    private const string InvalidEmailOrPasswordMessage = "Invalid e-mail or password.";
    private readonly RoleManager<BlogRole> _roleManager;
    private readonly ITokensService _tokensService;
    private readonly UserManager<BlogUser> _userManager;

    public AuthService(UserManager<BlogUser> userManager, ITokensService tokensService,
        RoleManager<BlogRole> roleManager)
    {
        _userManager = userManager;
        _tokensService = tokensService;
        _roleManager = roleManager;
    }

    public async Task<AuthenticationResult> Login(string email, string password)
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

        return await GenerateAuthenticationResultForUser(user);
    }

    public async Task Logout(RefreshToken token)
    {
        await _tokensService.RevokeRefreshToken(token);
    }

    public async Task<AuthenticationResult> RefreshTokens(ClaimsPrincipal principal, RefreshToken refreshToken)
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

        if (refreshToken.Used)
        {
            return new AuthenticationResult
            {
                Success = false,
                Errors = ["Refresh token was already used."]
            };
        }

        if (refreshToken.IsExpired)
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

        await _tokensService.UseRefreshToken(refreshToken);

        return await GenerateAuthenticationResultForUser(user);
    }

    public async Task<AuthenticationResult> Register(string displayName, string email, string password,
        IReadOnlyCollection<string>? roles = null)
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

        return await GenerateAuthenticationResultForUser(newUser);
    }

    private async Task<AuthenticationResult> GenerateAuthenticationResultForUser(BlogUser user)
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
        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        return new AuthenticationResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = user
        };
    }
}