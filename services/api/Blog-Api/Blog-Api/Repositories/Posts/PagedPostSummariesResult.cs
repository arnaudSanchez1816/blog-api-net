using BlogApi.Contracts.V1.Responses;

namespace BlogApi.Repositories.Posts;

public record PagedPostSummariesResult
{
    public required List<PostSummaryResponse> Posts { get; init; }
    public required int TotalCount { get; init; }
}