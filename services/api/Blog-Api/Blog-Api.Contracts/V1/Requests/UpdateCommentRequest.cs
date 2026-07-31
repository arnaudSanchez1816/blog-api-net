namespace BlogApi.Contracts.V1.Requests;

public record UpdateCommentRequest
{
    public string? Username { get; init; }

    public string? Body { get; init; }
}