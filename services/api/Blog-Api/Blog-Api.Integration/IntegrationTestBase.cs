using System.Security.Claims;
using BlogApi.Authorization;
using BlogApi.Domain;
using BlogApi.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private IAuthService _authService = null!;
    private RoleManager<BlogRole> _roleManager = null!;
    private AsyncServiceScope _scope;
    private UserManager<BlogUser> _userManager = null!;

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
        _roleManager = GetRequiredService<RoleManager<BlogRole>>();
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

    protected async Task CreateRoleWithPermissions(string roleName, IReadOnlyCollection<string> permissions)
    {
        BlogRole role = new BlogRole
        {
            Name = roleName
        };
        await _roleManager.CreateAsync(role);
        foreach (string permission in permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
        }
    }

    protected async Task<(BlogUser User, string BearerToken)> RegisterAuthenticatedUser(string displayName = "User",
        List<string>? roles = null)
    {
        string email = $"{Guid.NewGuid()}@email.com";
        AuthenticationResult result = await _authService.Register(displayName, email, "Password123", roles);
        BlogUser user = await _userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException();

        return (user, result.AccessToken!);
    }

    protected async Task<(BlogUser User, string BearerToken)> RegisterAuthenticatedUserWithPermissions(
        IReadOnlyCollection<string> permissions, string displayName = "User")
    {
        string roleName = $"Role_{Guid.NewGuid()}";
        await CreateRoleWithPermissions(roleName, permissions);

        string email = $"{Guid.NewGuid()}@email.com";
        AuthenticationResult result = await _authService.Register(displayName, email, "Password123", [roleName]);
        BlogUser user = await _userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException();

        return (user, result.AccessToken!);
    }
}