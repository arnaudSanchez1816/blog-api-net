using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Installers;

public static class IdentityInstaller
{
    public static IServiceCollection InstallIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>().BindConfiguration(nameof(IdentityOptions)).ValidateOnStart();
        services.AddIdentityCore<BlogUser>().AddRoles<BlogRole>().AddEntityFrameworkStores<DataContext>();

        return services;
    }
}