using BlogApi.Services.Comments;
using BlogApi.Services.Markdown;
using BlogApi.Services.Posts;
using BlogApi.Services.Tags;
using BlogApi.Services.Text;

namespace BlogApi.Installers;

public static class ServicesInstaller
{
    public static IServiceCollection InstallDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPostsService, PostsService>();
        services.AddScoped<ITagsService, TagsService>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddSingleton<ITextService, TextService>();
        services.AddSingleton<IMarkdownService, MarkdownService>();

        return services;
    }
}