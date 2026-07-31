namespace BlogApi.Contracts.V1.Responses;

public record CommentResponse
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string Body { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required Guid PostId { get; init; }
}