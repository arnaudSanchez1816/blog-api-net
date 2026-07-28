using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.Posts;

public class PostsRepository : IPostsRepository
{
    private readonly DataContext _context;

    public PostsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetPostBySlug(string slug)
    {
        return await _context.Posts
            .Include(x => x.Author)
            .SingleOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<Post?> GetPostBySlugWithTags(string slug)
    {
        return await _context.Posts
            .Include(x => x.Author)
            .Include(x => x.Tags)
            .SingleOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<IReadOnlyCollection<Post>> GetPostsStartingWithSlug(string slug)
    {
        return await _context.Posts
            .Where(p => p.Slug == slug || EF.Functions.Like(p.Slug, slug + "-%")).ToListAsync();
    }

    public async Task AddPost(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePost(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }
}