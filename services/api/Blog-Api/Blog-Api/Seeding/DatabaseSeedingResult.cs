namespace BlogApi.Seeding;

public record DatabaseSeedingResult
{
    public string? DevAccessToken { get; init; }
    public string? DevRefreshToken { get; init; }
}