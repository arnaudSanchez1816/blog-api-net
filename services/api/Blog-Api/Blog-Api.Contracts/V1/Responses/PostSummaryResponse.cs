namespace BlogApi.Contracts.V1.Responses;

/// <summary>
/// Summary of a post, generally in lists, does not include the post body
/// </summary>
public record PostSummaryResponse
{
    /// <summary>
    /// Guid of the post.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Title of the post.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The slug of this post, derived from the title
    /// </summary>
    /// <example>this-is-an-article-title</example>
    public required string Slug { get; init; }

    /// <summary>
    /// Description of the post.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Estimated reading time, in minutes.
    /// </summary>
    public required int ReadingTime { get; init; }

    /// <summary>
    /// Total number of comments for this post.
    /// </summary>
    public required int CommentsCount { get; init; }

    /// <summary>
    /// Date of the publication of the post. Is null when a post is unpublished.
    /// </summary>
    public required DateTimeOffset? PublishedAt { get; init; }

    /// <summary>
    /// Details of the post author.
    /// </summary>
    public required PostAuthorResponse Author { get; init; }

    /// <summary>
    /// Tags of the post.
    /// </summary>
    public required ICollection<TagResponse> Tags { get; init; }
}