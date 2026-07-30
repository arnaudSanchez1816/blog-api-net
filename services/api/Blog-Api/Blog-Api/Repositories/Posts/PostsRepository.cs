using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Requests.Queries;
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

    public async Task<List<Post>> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination)
    {
        IQueryable<Post> postsQuery = _context.Posts.AsQueryable();

        // Use default filters if none are provided
        filter ??= new GetPostsFilterQuery();
        // Filters
        postsQuery = ApplyGetPostsFilters(filter, postsQuery);

        if (pagination is null)
        {
            return await postsQuery.ToListAsync();
        }

        // Pagination
        (int pageNumber, int pageSize) = pagination;
        int skip = (pageNumber - 1) * pageSize;

        return await postsQuery.Skip(skip).Take(pageSize).ToListAsync();
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

    public async Task UpdatePost(Post post)
    {
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePost(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Post> ApplyGetPostsFilters(GetPostsFilterQuery filter, IQueryable<Post> postsQuery)
    {
        (string q, PostSortOption sortBy, IReadOnlyCollection<string>? tags, bool includeUnpublished) = filter;

        if (!string.IsNullOrWhiteSpace(q))
        {
            postsQuery = postsQuery.Where(p => p.Title == q || EF.Functions.ILike(p.Title, $"%{q}%"));
        }

        if (!includeUnpublished)
        {
            postsQuery = postsQuery.Where(p => p.PublishedAt != null);
        }

        if (tags != null && tags.Count > 0)
        {
            postsQuery = postsQuery.Where(p => p.Tags.Any(t => tags.Contains(t.Slug)));
        }

        postsQuery = sortBy switch
        {
            PostSortOption.IdAscending => postsQuery.OrderBy(p => p.Id),
            PostSortOption.IdDescending => postsQuery.OrderByDescending(p => p.Id),
            PostSortOption.PublishedAtAscending => postsQuery.OrderBy(p => p.PublishedAt).ThenBy(p => p.Id),
            PostSortOption.PublishedAtDescending => postsQuery.OrderByDescending(p => p.PublishedAt)
                .ThenBy(p => p.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported sort value")
        };

        return postsQuery;
    }
}