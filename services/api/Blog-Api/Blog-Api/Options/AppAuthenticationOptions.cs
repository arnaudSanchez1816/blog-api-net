using System.ComponentModel.DataAnnotations;

namespace BlogApi.Options;

public class AppAuthenticationOptions
{
    public const string ConfigurationSection = "AppAuthenticationOptions";

    [Required]
    [MinLength(32)]
    public required string JwtAccessSecret { get; init; }

    [Required]
    public required Uri JwtIssuerUri { get; init; }

    [Required]
    public required Uri JwtAudienceUri { get; init; }
}