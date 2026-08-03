namespace BlogApi.Services.Auth;

public interface IAuthService
{
    public Task<AuthenticationResult> Login(string email, string password);

    public Task<AuthenticationResult> Register(string username, string email, string password);
}