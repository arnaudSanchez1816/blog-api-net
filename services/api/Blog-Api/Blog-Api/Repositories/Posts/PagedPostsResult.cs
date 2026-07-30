using BlogApi.Domain;

namespace BlogApi.Repositories.Posts;

public record PagedPostsResult
{
    public required List<Post> Posts { get; init; }
    public required int TotalCount { get; init; }
}
