using System.ComponentModel.DataAnnotations;

namespace BlogApi.Domain;

public class BaseEntity
{
    [Key]
    public Guid Id { get; init; }
}