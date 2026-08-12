using BlogApi.Services.Auth;
using BlogApi.Services.Comments;
using BlogApi.Services.Markdown;
using BlogApi.Services.Posts;
using BlogApi.Services.Tags;
using BlogApi.Services.Text;
using BlogApi.Services.Tokens;

namespace BlogApi.Installers;

public static class ServicesInstaller
{
    public static IServiceCollection InstallDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPostsService, PostsService>();
        services.AddScoped<ITagsService, TagsService>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddScoped<ITokensService, TokensService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ITextService, TextService>();
        services.AddSingleton<IMarkdownService, MarkdownService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection InstallBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<RefreshTokensCleanupService>();
        return services;
    }
}