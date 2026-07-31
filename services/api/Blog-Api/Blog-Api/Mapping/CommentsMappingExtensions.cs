using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;

namespace BlogApi.Mapping;

public static class CommentsMappingExtensions
{
    public static CommentResponse ToCommentResponse(this Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            Body = comment.Body,
            Username = comment.Username,
            CreatedAt = comment.CreatedAt,
            PostId = comment.PostId
        };
    }
}