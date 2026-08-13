namespace BlogApi.Options;

public class AppDataProtectionOptions
{
    public const string ConfigurationSection = "AppDataProtectionOptions";

    public string? KeysPath { get; init; }
}
