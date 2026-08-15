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

    public async Task<Comment?> GetCommentById(Guid id, CancellationToken ct = default)
    {
        return await _commentsRepository.GetCommentById(id, ct);
    }

    public async Task<List<Comment>> GetAllCommentsWithPostId(Guid postId, CancellationToken ct = default)
    {
        return await _commentsRepository.GetAllCommentsWithPostId(postId, ct);
    }

    public async Task<Comment> CreateComment(string username, string body, Guid postId, CancellationToken ct = default)
    {
        Comment newComment = new Comment
        {
            Body = body,
            Username = username,
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = postId
        };

        await _commentsRepository.AddComment(newComment, ct);
        return newComment;
    }

    public async Task DeleteComment(Comment comment, CancellationToken ct = default)
    {
        await _commentsRepository.DeleteComment(comment, ct);
    }

    public async Task UpdateComment(Comment comment, string? username, string? body, CancellationToken ct = default)
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

        await _commentsRepository.UpdateComment(comment, ct);
    }
}