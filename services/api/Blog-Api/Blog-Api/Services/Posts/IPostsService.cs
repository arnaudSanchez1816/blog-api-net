using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Domain;
using BlogApi.Repositories.Posts;

namespace BlogApi.Services.Posts;

public interface IPostsService
{
    public Task<PagedPostSummariesResult> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination,
        CancellationToken ct = default);

    public Task<Post?> GetPostBySlug(string slug, CancellationToken ct = default);
    public Task<Post?> GetPostBySlugWithTags(string slug, CancellationToken ct = default);
    public Task<Post?> GetPostBySlugWithComments(string slug, CancellationToken ct = default);
    public Task<Post> CreatePost(Post post, CancellationToken ct = default);
    public Task<Post> CreatePost(string title, Guid authorId, CancellationToken ct = default);
    public Task UpdatePost(Post post, UpdatePostRequest updatePostDto, CancellationToken ct = default);
    public Task DeletePost(Post post, CancellationToken ct = default);
    public Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct = default);
    public Task<Comment> CreateCommentForPost(Post post, string username, string body, CancellationToken ct = default);
}