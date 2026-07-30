using BlogApi.Repositories.Comments;
using BlogApi.Repositories.Posts;
using BlogApi.Repositories.Tags;

namespace BlogApi.Installers;

public static class RepositoriesInstaller
{
    public static IServiceCollection InstallRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITagsRepository, TagsRepository>();
        services.AddScoped<IPostsRepository, PostsRepository>();
        services.AddScoped<ICommentsRepository, CommentsRepository>();

        return services;
    }
}