using BlogApi.Domain;
using BlogApi.Repositories.Comments;

namespace BlogApi.Services.Comments;

public class CommentsService : ICommentsService
{
    private readonly ICommentsRepository _commentsRepository;

    public CommentsService(ICommentsRepository commentsRepository)
    {
        _commentsRepository = commentsRepository;
    }

    public async Task<Comment?> GetCommentById(Guid id)
    {
        return await _commentsRepository.GetCommentById(id);
    }

    public async Task<List<Comment>> GetAllCommentsWithPostId(Guid postId)
    {
        return await _commentsRepository.GetAllCommentsWithPostId(postId);
    }

    public async Task<Comment> CreateComment(string username, string body, Guid postId)
    {
        Comment newComment = new Comment
        {
            Body = body,
            Username = username,
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = postId
        };

        await _commentsRepository.AddComment(newComment);
        return newComment;
    }

    public async Task DeleteComment(Comment comment)
    {
        await _commentsRepository.DeleteComment(comment);
    }

    public async Task UpdateComment(Comment comment, string? username, string? body)
    {
        if (username is null && body is null)
        {
            return;
        }

        if (username is not null)
        {
            comment.Username = username;
        }

        if (body is not null)
        {
            comment.Body = body;
        }

        await _commentsRepository.UpdateComment(comment);
    }
}