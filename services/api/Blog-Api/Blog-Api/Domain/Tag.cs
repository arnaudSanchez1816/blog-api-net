using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Domain;

[Index(nameof(Slug), IsUnique = true)]
public class Tag : BaseEntity
{
    public const int TagNameMaxLength = 64;
    public const int TagSlugMaxLength = 64;

    [MaxLength(TagNameMaxLength)]
    public required string Name { get; set; }

    [MaxLength(TagSlugMaxLength)]
    public required string Slug { get; set; }
}