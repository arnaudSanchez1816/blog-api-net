using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Domain;

namespace BlogApi.Repositories.Posts;

public interface IPostsRepository
{
    public Task<PagedPostsResult> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination);
    public Task<Post?> GetPostBySlug(string slug);
    public Task<Post?> GetPostBySlugWithTags(string slug);
    public Task<IReadOnlyCollection<Post>> GetPostsStartingWithSlug(string slug);
    public Task AddPost(Post post);
    public Task UpdatePost(Post post);
    public Task DeletePost(Post post);
}