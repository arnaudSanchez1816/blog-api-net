using BlogApi.Domain;
using BlogApi.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private IAuthService _authService;
    private AsyncServiceScope _scope;
    private UserManager<BlogUser> _userManager;

    protected BlogApiFactory Factory { get; }

    protected HttpClient HttpClient
    {
        get => Factory.HttpClient;
    }

    public IntegrationTestBase(BlogApiFactory factory)
    {
        Factory = factory;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        await _scope.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        _scope = Factory.Services.CreateAsyncScope();
        _userManager = GetRequiredService<UserManager<BlogUser>>();
        _authService = GetRequiredService<IAuthService>();
        await OnInitializeAsync();
    }

    protected virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    protected async Task<(BlogUser User, string BearerToken)> RegisterAuthenticatedUser(string displayName = "User",
        List<string>? roles = null)
    {
        string email = $"{Guid.NewGuid()}@email.com";
        AuthenticationResult result = await _authService.Register(displayName, email, "Password123");
        BlogUser user = await _userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException();
        if (roles is not null)
        {
            await _userManager.AddToRolesAsync(user, roles);
        }

        return (user, result.AccessToken!);
    }
}