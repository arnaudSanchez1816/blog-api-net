using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApi.Domain;

public class Post : BaseEntity
{
    [MaxLength(300)]
    public required string Title { get; set; }

    public string Description { get; set; } = "New post description";

    [MaxLength(220)]
    public required string Slug { get; set; }

    public string Body { get; set; } = "New post body";

    public int ReadingTime { get; set; } = 1;

    public DateTimeOffset? PublishedAt { get; set; }

    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public BlogUser User { get; set; } = null!;

    public ICollection<Tag> Tags { get; } = new List<Tag>();
}