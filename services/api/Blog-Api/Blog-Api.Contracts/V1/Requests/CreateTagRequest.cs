namespace BlogApi.Contracts.V1.Requests;

public record CreateTagRequest
{
    public required string Name { get; init; }

    public required string Slug { get; init; }
}