namespace BlogApi.Contracts.Health;

public record HealthCheckResponse
{
    public required string Status { get; init; }
    public List<HealthCheck> Checks { get; init; } = [];
    public required TimeSpan Duration { get; init; }
}