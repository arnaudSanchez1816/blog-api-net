using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;

namespace BlogApi.Mapping;

public static class TagsMappingExtensions
{
    public static Tag ToTag(this CreateTagRequest request)
    {
        return new Tag
        {
            Name = request.Name,
            Slug = request.Slug
        };
    }

    public static TagResponse ToTagResponse(this Tag tag)
    {
        return new TagResponse
        {
            Id = tag.Id,
            Slug = tag.Slug,
            Name = tag.Name
        };
    }
}