using BlogApi.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Installers;

public static class AuthorizationInstaller
{
    public static IServiceCollection InstallAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PostOwnerAuthorizationHandler>();

        return services;
    }
}