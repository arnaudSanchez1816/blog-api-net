using System.Linq.Expressions;
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

    public async Task<PagedPostsResult> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination,
        CancellationToken ct = default)
    {
        IQueryable<Post> postsQuery = _context.Posts.AsQueryable()
            .Include(p => p.Author)
            .Include(p => p.Tags);

        // Use default filters if none are provided
        filter ??= new GetPostsFilterQuery();
        // Filters
        postsQuery = ApplyGetPostsFilters(filter, postsQuery);

        int totalCount = await postsQuery.CountAsync(ct);

        if (pagination is null)
        {
            List<Post> allPosts = await ToPostsWithCommentsCountAsync(postsQuery, ct);

            return new PagedPostsResult { Posts = allPosts, TotalCount = totalCount };
        }

        // Pagination
        (int pageNumber, int pageSize) = pagination;
        int skip = (pageNumber - 1) * pageSize;

        List<Post> pagedPosts = await ToPostsWithCommentsCountAsync(postsQuery.Skip(skip).Take(pageSize), ct);

        return new PagedPostsResult { Posts = pagedPosts, TotalCount = totalCount };
    }

    public async Task<Post?> GetPostBySlug(string slug, CancellationToken ct = default)
    {
        return await ToPostWithCommentsCountAsync(_context.Posts
                .Include(x => x.Author),
            x => x.Slug == slug,
            ct);
    }

    public async Task<Post?> GetPostBySlugWithTags(string slug, CancellationToken ct = default)
    {
        return await ToPostWithCommentsCountAsync(_context.Posts
                .Include(x => x.Author)
                .Include(x => x.Tags),
            x => x.Slug == slug,
            ct);
    }

    public async Task<Post?> GetPostBySlugWithComments(string slug, CancellationToken ct = default)
    {
        Post? post = await _context.Posts
            .Include(x => x.Author)
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .SingleOrDefaultAsync(x => x.Slug == slug, ct);
        if (post == null)
        {
            return null;
        }

        post.CommentsCount = post.Comments.Count;
        return post;
    }

    public async Task<IReadOnlyCollection<string>> GetSlugsStartingWithSlug(string slug, CancellationToken ct = default)
    {
        return await _context.Posts
            .Where(p => p.Slug == slug || EF.Functions.Like(p.Slug, slug + "-%"))
            .Select(p => p.Slug)
            .ToListAsync(ct);
    }

    public async Task AddPost(Post post, CancellationToken ct = default)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync(ct);
        await _context.Entry(post).Reference(p => p.Author).LoadAsync(ct);
    }

    public async Task UpdatePost(Post post, CancellationToken ct = default)
    {
        _context.Posts.Update(post);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeletePost(Post post, CancellationToken ct = default)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync(ct);
    }

    private static IQueryable<Post> ApplyGetPostsFilters(GetPostsFilterQuery filter, IQueryable<Post> postsQuery)
    {
        (string q, PostSortOption sortBy, IReadOnlyCollection<string>? tags, bool includeUnpublished, Guid? authorId) =
            filter;

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

        if (authorId is not null)
        {
            postsQuery = postsQuery.Where(p => p.AuthorId == authorId);
        }

        postsQuery = sortBy switch
        {
            PostSortOption.IdAscending => postsQuery.OrderBy(p => p.Id),
            PostSortOption.IdDescending => postsQuery.OrderByDescending(p => p.Id),
            PostSortOption.PublishedAtAscending => postsQuery.OrderBy(p => p.PublishedAt)
                .ThenBy(p => p.Id),
            PostSortOption.PublishedAtDescending => postsQuery.OrderByDescending(p => p.PublishedAt)
                .ThenBy(p => p.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported sort value")
        };

        return postsQuery;
    }

    private static async Task<List<Post>> ToPostsWithCommentsCountAsync(IQueryable<Post> query,
        CancellationToken ct = default)
    {
        var projected = await query
            .Select(p => new { Post = p, CommentsCount = p.Comments.Count() })
            .ToListAsync(ct);

        foreach (var p in projected)
        {
            p.Post.CommentsCount = p.CommentsCount;
        }

        return projected.Select(x => x.Post).ToList();
    }

    private static async Task<Post?> ToPostWithCommentsCountAsync(
        IQueryable<Post> query, Expression<Func<Post, bool>> predicate, CancellationToken ct = default)
    {
        var result = await query
            .Where(predicate)
            .Select(p => new { Post = p, CommentsCount = p.Comments.Count() })
            .SingleOrDefaultAsync(ct);

        if (result is null)
        {
            return null;
        }

        result.Post.CommentsCount = result.CommentsCount;
        return result.Post;
    }
}