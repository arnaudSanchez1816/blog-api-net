using BlogApi.Domain;

namespace BlogApi.Services.Posts;

public interface IPostsService
{
    public Task<Post?> GetPostBySlug(string slug);
    public Task<Post?> GetPostBySlugWithTags(string slug);
    public Task<Post> CreatePost(Post post);
    public Task UpdatePost(Post post);
    public Task DeletePost(Post post);
    public Task<string> GenerateUniqueSlugAsync(string title);
}