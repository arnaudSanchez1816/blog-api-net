using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Domain;

namespace BlogApi.Repositories.Posts;

public interface IPostsRepository
{
    public Task<PagedPostsResult> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination,
        CancellationToken ct = default);

    public Task<Post?> GetPostBySlug(string slug, CancellationToken ct = default);
    public Task<Post?> GetPostBySlugWithTags(string slug, CancellationToken ct = default);
    public Task<Post?> GetPostBySlugWithComments(string slug, CancellationToken ct = default);
    public Task<IReadOnlyCollection<string>> GetSlugsStartingWithSlug(string slug, CancellationToken ct = default);
    public Task AddPost(Post post, CancellationToken ct = default);
    public Task UpdatePost(Post post, CancellationToken ct = default);
    public Task DeletePost(Post post, CancellationToken ct = default);
}