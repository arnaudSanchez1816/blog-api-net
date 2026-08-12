namespace BlogApi.Contracts.V1.Responses;

public record LoginResponse
{
    public required string AccessToken { get; init; }

    public required UserResponse User { get; init; }
}