using System.ComponentModel.DataAnnotations;
using BlogApi.Utils;

namespace BlogApi.Contracts.V1.Responses;

public record PostResponse
{
    /// <summary>
    /// Guid of the post.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Title of the post.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The slug of this post, derived from the title
    /// </summary>
    /// <example>this-is-an-article-title</example>
    [RegularExpression(SlugGenerator.Pattern)]
    public string Slug { get; }

    /// <summary>
    /// Content of the post. In a Markdown format.
    /// </summary>
    public string Body { get; }

    public PostResponse(Guid Id, string Title, string Slug, string Body)
    {
        this.Id = Id;
        this.Title = Title;
        this.Slug = Slug;
        this.Body = Body;
    }

    public void Deconstruct(out Guid id, out string title, out string slug, out string body)
    {
        id = Id;
        title = Title;
        slug = Slug;
        body = Body;
    }
}