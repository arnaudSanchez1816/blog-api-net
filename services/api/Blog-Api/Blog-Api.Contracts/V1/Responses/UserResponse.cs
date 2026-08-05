namespace BlogApi.Contracts.V1.Responses;

public record UserResponse
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Name { get; init; }
}