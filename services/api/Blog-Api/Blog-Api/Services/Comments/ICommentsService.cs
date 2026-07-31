using BlogApi.Domain;

namespace BlogApi.Services.Comments;

public interface ICommentsService
{
    public Task<Comment?> GetCommentById(Guid id);
    public Task<List<Comment>> GetAllCommentsWithPostId(Guid postId);

    public Task<Comment> CreateComment(string username, string body, Guid postId);
    public Task UpdateComment(Comment comment, string? username, string? body);
    public Task DeleteComment(Comment comment);
}