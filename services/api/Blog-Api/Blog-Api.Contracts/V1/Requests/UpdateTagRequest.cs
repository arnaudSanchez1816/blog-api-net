namespace BlogApi.Contracts.V1.Requests;

public record UpdateTagRequest
{
    public string? Name { get; init; }

    /// <summary>
    /// New slug of the tag.
    /// </summary>
    /// <example>chocolate-recipe</example>
    public string? Slug { get; init; }
}