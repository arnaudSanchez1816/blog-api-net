using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Services.Posts;

public class PostsService : IPostsService
{
    private readonly DataContext _dataContext;

    public PostsService(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Post?> GetPostBySlug(string slug)
    {
        return await _dataContext.Posts.AsNoTracking()
            .Include(x => x.Author)
            .SingleOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<Post?> GetPostBySlugWithTags(string slug)
    {
        return await _dataContext.Posts.AsNoTracking()
            .Include(x => x.Author)
            .Include(x => x.Tags)
            .SingleOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<string> GenerateUniqueSlugAsync(string title)
    {
        string baseSlug = SlugGenerator.Generate(title);

        List<string> takenSlugs = await _dataContext.Posts
            .Where(p => p.Slug == baseSlug || EF.Functions.Like(p.Slug, baseSlug + "-%"))
            .Select(p => p.Slug)
            .ToListAsync();

        if (takenSlugs.Count == 0)
        {
            return baseSlug;
        }

        int nextSuffix = takenSlugs
            .Select(slug => slug[(baseSlug.Length + 1)..])
            .Where(rest => rest.Length > 0 && rest.All(char.IsDigit))
            .Select(int.Parse)
            .DefaultIfEmpty(1)
            .Max() + 1;

        return $"{baseSlug}-{nextSuffix}";
    }
}