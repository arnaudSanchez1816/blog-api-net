namespace BlogApi.Contracts.V1.Responses;

public record LoginResponse
{
    public required string AccessToken { get; init; }
}