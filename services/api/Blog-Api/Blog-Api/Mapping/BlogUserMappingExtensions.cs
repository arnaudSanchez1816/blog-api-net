using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;

namespace BlogApi.Mapping;

public static class BlogUserMappingExtensions
{
    public static PostAuthorResponse ToPostAuthorResponse(this BlogUser user)
    {
        return new PostAuthorResponse
        {
            Id = user.Id,
            Name = user.DisplayName
        };
    }
}