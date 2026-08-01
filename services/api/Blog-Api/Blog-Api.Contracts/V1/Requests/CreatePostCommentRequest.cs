namespace BlogApi.Contracts.V1.Requests;

public record CreatePostCommentRequest
{
    public required string Body { get; init; }
    public required string Username { get; init; }
}