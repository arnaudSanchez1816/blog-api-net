using System.ComponentModel.DataAnnotations;
using BlogApi.Contracts.V1.Responses;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Domain;

[Index(nameof(Slug), IsUnique = true)]
public class Tag : BaseEntity
{
    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(64)]
    public required string Slug { get; set; }

    public TagResponse ToTagResponse()
    {
        return new TagResponse
        {
            Id = Id,
            Name = Name,
            Slug = Slug
        };
    }
}