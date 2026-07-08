using BlogApi.Data;
using BlogApi.Domain;

namespace BlogApi.Installers;

public static class IdentityInstaller
{
    public static IServiceCollection InstallIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<BlogUser>().AddRoles<BlogRole>().AddEntityFrameworkStores<DataContext>();
        return services;
    }
}