using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApi.Domain;

public class RefreshToken
{
    public static readonly TimeSpan UsedGracePeriod = TimeSpan.FromSeconds(15);

    [Key]
    [MaxLength(256)]
    public required string Token { get; init; }

    public required DateTimeOffset CreationDate { get; init; }
    public required DateTimeOffset ExpirationDate { get; init; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }

    /// <summary>
    /// Used to determine a token grace period to fix front end race conditions (Like React strict mode) due to token single
    /// usage.
    /// </summary>
    public DateTimeOffset? UsedDate { get; set; }

    public required Guid UserId { get; init; }

    [ForeignKey(nameof(UserId))]
    public BlogUser User { get; init; } = null!;

    public RefreshToken? ReplacedByToken { get; set; }

    /// <summary>
    /// To handle race condition where two calls to rotate a refresh token are simultanous
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpirationDate;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return !Invalidated && (!Used || IsWithinGracePeriod(now)) && !IsExpired(now);
    }

    public bool IsWithinGracePeriod(DateTimeOffset now)
    {
        return Used && UsedDate?.Add(UsedGracePeriod) > now;
    }
}