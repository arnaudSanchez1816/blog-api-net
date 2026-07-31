using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApi.Domain;

public class Comment : BaseEntity
{
    public const int BodyMaxLength = 3000;

    [MaxLength(256)]
    public required string Username { get; set; }

    [MaxLength(BodyMaxLength)]
    public required string Body { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required Guid PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public Post Post { get; set; } = null!;
}