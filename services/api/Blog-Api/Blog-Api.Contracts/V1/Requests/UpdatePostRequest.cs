namespace BlogApi.Contracts.V1.Requests;

public record UpdatePostRequest
{
    public string? Title { get; init; }
    public string? Body { get; init; }
    public IReadOnlyCollection<string>? Tags { get; init; }
}