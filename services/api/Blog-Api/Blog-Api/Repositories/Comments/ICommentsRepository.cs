using BlogApi.Domain;

namespace BlogApi.Repositories.Comments;

public interface ICommentsRepository
{
    public Task<Comment?> GetCommentById(Guid id);

    public Task<List<Comment>> GetAllCommentsWithPostId(Guid postId);
}