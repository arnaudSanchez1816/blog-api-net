namespace BlogApi.Contracts.V1.Responses;

public record PostResponse
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
    /// Content of the post. In a Markdown format.
    /// </summary>
    public required string Body { get; init; }

    public required ICollection<TagResponse> Tags { get; init; }

    public PostResponse()
    {
    }

    public PostResponse(Guid id, string title, string slug, string body, ICollection<TagResponse> tags)
    {
        Id = id;
        Title = title;
        Slug = slug;
        Body = body;
        Tags = tags;
    }

    public void Deconstruct(out Guid id, out string title, out string slug, out string body,
        out ICollection<TagResponse> tags)
    {
        id = Id;
        title = Title;
        slug = Slug;
        body = Body;
        tags = Tags;
    }
}