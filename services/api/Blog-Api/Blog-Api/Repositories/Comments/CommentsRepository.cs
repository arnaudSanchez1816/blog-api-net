using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.Comments;

public class CommentsRepository : ICommentsRepository
{
    private readonly DataContext _context;

    public CommentsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetCommentById(Guid id)
    {
        return await _context.Comments.SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Comment>> GetAllCommentsWithPostId(Guid postId)
    {
        return await _context.Comments.Where(c => c.PostId == postId).ToListAsync();
    }
}