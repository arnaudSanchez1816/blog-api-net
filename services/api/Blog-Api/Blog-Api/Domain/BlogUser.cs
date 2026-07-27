using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Domain;

public class BlogUser : IdentityUser<Guid>
{
    [MaxLength(256)]
    public required string DisplayName { get; set; }

    public ICollection<Post> Posts { get; } = new List<Post>();
}