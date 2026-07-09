using BlogApi.Services;

namespace BlogApi.Installers;

public static class ServicesInstaller
{
    public static IServiceCollection InstallDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPostsService, PostsService>();

        return services;
    }
}