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

    public async Task<Comment?> GetCommentById(Guid id, CancellationToken ct = default)
    {
        return await _context.Comments.SingleOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Comment>> GetAllCommentsWithPostId(Guid postId, CancellationToken ct = default)
    {
        return await _context.Comments.Where(c => c.PostId == postId).ToListAsync(ct);
    }

    public async Task AddComment(Comment comment, CancellationToken ct = default)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateComment(Comment commentToUpdate, CancellationToken ct = default)
    {
        _context.Comments.Update(commentToUpdate);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteComment(Comment commentToDelete, CancellationToken ct = default)
    {
        _context.Comments.Remove(commentToDelete);
        await _context.SaveChangesAsync(ct);
    }
}