using BlogApi.Services;
using BlogApi.Services.Comments;
using BlogApi.Services.Posts;
using BlogApi.Services.Tags;

namespace BlogApi.Installers;

public static class ServicesInstaller
{
    public static IServiceCollection InstallDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPostsService, PostsService>();
        services.AddScoped<ITagsService, TagsService>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddSingleton<TextService>();

        return services;
    }
}