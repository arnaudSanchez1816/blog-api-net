namespace BlogApi.Contracts.V1.Requests;

public record UpdatePostRequest
{
    public required string? Title { get; init; }
    public required string? Body { get; init; }
    public required IReadOnlyCollection<string>? Tags { get; init; }
}