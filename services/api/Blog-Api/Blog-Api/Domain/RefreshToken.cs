using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApi.Domain;

public class RefreshToken
{
    [Key]
    [MaxLength(256)]
    public required string Token { get; init; }

    public required DateTimeOffset CreationDate { get; init; }
    public required DateTimeOffset ExpirationDate { get; init; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }

    public required Guid UserId { get; init; }

    [ForeignKey(nameof(UserId))]
    public BlogUser User { get; init; } = null!;

    public bool IsExpired
    {
        get => DateTimeOffset.UtcNow >= ExpirationDate;
    }

    public bool IsActive
    {
        get => !Invalidated && !Used && !IsExpired;
    }
}