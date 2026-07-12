using BlogApi.Repositories.Tags;

namespace BlogApi.Installers;

public static class RepositoriesInstaller
{
    public static IServiceCollection InstallRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITagsRepository, TagsRepository>();
        return services;
    }
}