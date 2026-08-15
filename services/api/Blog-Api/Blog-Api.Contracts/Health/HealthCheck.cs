namespace BlogApi.Contracts.Health;

public record HealthCheck
{
    public required string Status { get; set; }
    public required string Component { get; set; }
    public required string Description { get; set; }
}