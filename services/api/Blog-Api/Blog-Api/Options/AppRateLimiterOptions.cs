namespace BlogApi.Options;

public class AppRateLimiterOptions
{
    public const string ConfigurationSection = "AppRateLimiterOptions";

    public bool Enabled { get; init; } = true;
    public required int GlobalPermitLimit { get; set; } = 100;
    public required int AuthPermitLimit { get; set; } = 5;
}