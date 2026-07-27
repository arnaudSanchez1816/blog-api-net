namespace BlogApi.Contracts.V1.Requests;

public record CreatePostRequest
{
    public required string Title { get; init; }
}