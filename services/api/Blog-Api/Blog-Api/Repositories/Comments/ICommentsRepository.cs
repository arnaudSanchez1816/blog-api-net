using BlogApi.Domain;

namespace BlogApi.Repositories.Comments;

public interface ICommentsRepository
{
    public Task<Comment?> GetCommentById(Guid id, CancellationToken ct = default);

    public Task<List<Comment>> GetAllCommentsWithPostId(Guid postId, CancellationToken ct = default);

    public Task AddComment(Comment comment, CancellationToken ct = default);
    public Task UpdateComment(Comment commentToUpdate, CancellationToken ct = default);
    public Task DeleteComment(Comment commentToDelete, CancellationToken ct = default);
}