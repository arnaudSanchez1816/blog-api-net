using System.ComponentModel.DataAnnotations;

namespace BlogApi.Options;

public class DatabaseSeedingOptions
{
    public const string ConfigurationSection = "DatabaseSeedingOptions";

    public bool Enabled { get; init; } = false;

    [Required]
    public required string AdminName { get; init; }

    [Required]
    [EmailAddress]
    public required string AdminEmail { get; init; }

    [Required]
    public required string AdminPassword { get; init; }
}