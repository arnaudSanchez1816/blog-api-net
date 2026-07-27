using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;

namespace BlogApi.Mapping;

public static class PostsMappingExtensions
{
    public static PostResponse ToPostResponse(this Post post)
    {
        return new PostResponse
        {
            Id = post.Id,
            Slug = post.Slug,
            Title = post.Title,
            Body = post.Body,
            Tags = post.Tags.Select(x => x.ToTagResponse()).ToList()
        };
    }
}