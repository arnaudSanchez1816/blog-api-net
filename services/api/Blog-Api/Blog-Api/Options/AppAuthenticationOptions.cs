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

    public TimeSpan RefreshTokensExpirationBuffer { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan RefreshTokensCleanupInterval { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(5);
}