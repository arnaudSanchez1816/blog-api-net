using BlogApi.Domain;

namespace BlogApi.Services.Comments;

public interface ICommentsService
{
    public Task<Comment?> GetCommentById(Guid id, CancellationToken ct = default);
    public Task<List<Comment>> GetAllCommentsWithPostId(Guid postId, CancellationToken ct = default);

    public Task<Comment> CreateComment(string username, string body, Guid postId, CancellationToken ct = default);
    public Task UpdateComment(Comment comment, string? username, string? body, CancellationToken ct = default);
    public Task DeleteComment(Comment comment, CancellationToken ct = default);
}