using BlogApi.Domain;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Services.Auth;

public class AuthService : IAuthService
{
    private const string InvalidEmailOrPasswordMessage = "Invalid e-mail or password.";
    private readonly ITokensService _tokensService;
    private readonly UserManager<BlogUser> _userManager;

    public AuthService(UserManager<BlogUser> userManager, ITokensService tokensService)
    {
        _userManager = userManager;
        _tokensService = tokensService;
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

        string accessToken = _tokensService.GenerateAccessToken(user);
        return new AuthenticationResult
        {
            Success = true,
            AccessToken = accessToken
        };
    }

    public async Task<AuthenticationResult> Register(string username, string email, string password)
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
            UserName = username,
            DisplayName = username,
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

        string accessToken = _tokensService.GenerateAccessToken(newUser);
        return new AuthenticationResult
        {
            Success = true,
            AccessToken = accessToken
        };
    }
}