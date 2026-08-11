namespace BlogApi.Options;

public class CorsOptions
{
    public const string ConfigurationSection = "CorsOptions";

    public required IReadOnlyCollection<string> AllowedOrigins { get; init; } = [];
    public required bool AllowAllOrigins { get; init; } = false;
}