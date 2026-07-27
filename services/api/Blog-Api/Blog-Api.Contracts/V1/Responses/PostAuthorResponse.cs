namespace BlogApi.Contracts.V1.Responses;

public record PostAuthorResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}