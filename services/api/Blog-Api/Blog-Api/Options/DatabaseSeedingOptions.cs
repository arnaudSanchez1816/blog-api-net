using System.ComponentModel.DataAnnotations;

namespace BlogApi.Options;

public class DatabaseSeedingOptions
{
    public const string ConfigurationSection = "DatabaseSeedingOptions";

    [Required]
    public required string AdminName { get; init; }

    [Required]
    [EmailAddress]
    public required string AdminEmail { get; init; }

    [Required]
    public required string AdminPassword { get; init; }
}