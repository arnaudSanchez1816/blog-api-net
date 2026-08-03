namespace BlogApi.Contracts.V1.Responses;

public record GetAccessTokenResponse
{
    public required string AccessToken { get; init; }
}