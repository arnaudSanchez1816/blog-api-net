using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApi.Domain;

public class Post : BaseEntity
{
    public const int TitleMaxLength = 300;
    public const int SlugMaxLength = 220;

    [MaxLength(TitleMaxLength)]
    public required string Title { get; set; }

    public string Description { get; set; } = "New post description";

    [MaxLength(SlugMaxLength)]
    public required string Slug { get; set; }

    public string Body { get; set; } = "New post body";

    public int ReadingTime { get; set; } = 1;

    public DateTimeOffset? PublishedAt { get; set; }

    public required Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public BlogUser Author { get; set; } = null!;

    public ICollection<Tag> Tags { get; } = new List<Tag>();
}