using System.ComponentModel.DataAnnotations;

namespace BlogApi.Domain;

public class Post : BaseEntity
{
    [MaxLength(300)]
    public required string Title { get; set; }

    [MaxLength(220)]
    public required string Slug { get; set; }

    public required string Body { get; set; }
}