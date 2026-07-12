namespace BlogApi.Contracts.V1.Responses;

public record TagResponse
{
    /// <summary>
    /// Id of the tag.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the tag.
    /// </summary>
    /// <example>Chocolate recipes</example>
    public required string Name { get; init; }

    /// <summary>
    /// Unique slug identifier of the tag.
    /// </summary>
    /// <example>chocolate-recipes</example>
    public required string Slug { get; init; }
}