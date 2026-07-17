using BlogApi.Contracts.V1.Requests;
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
}