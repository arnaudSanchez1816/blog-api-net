using BlogApi.Domain;

namespace BlogApi.Services;

public interface IPostsService
{
    public Task<Post?> GetPostBySlug(string slug);
    public Task<string> GenerateUniqueSlugAsync(string title);
}