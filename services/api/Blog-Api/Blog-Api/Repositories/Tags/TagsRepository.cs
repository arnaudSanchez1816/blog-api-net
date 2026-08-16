using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Exceptions;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.Tags;

public class TagsRepository : ITagsRepository
{
    private readonly DataContext _context;

    public TagsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Tag>> GetAllTags(CancellationToken ct = default)
    {
        return await _context.Tags.ToListAsync(ct);
    }

    public async Task<Tag?> GetTagBySlug(string slug, CancellationToken ct = default)
    {
        return await _context.Tags.SingleOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public async Task<Tag?> GetTagById(Guid id, CancellationToken ct = default)
    {
        return await _context.Tags.SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<Tag>> GetAllTagsBySlug(IReadOnlyCollection<string> slugs, CancellationToken ct = default)
    {
        return await _context.Tags.Where(x => slugs.Contains(x.Slug)).ToListAsync(ct);
    }

    public async Task AddTag(Tag newTag, CancellationToken ct = default)
    {
        try
        {
            _context.Tags.Add(newTag);
            await _context.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException e)
        {
            bool isUniqueSlugConstraint = e.ConstraintProperties.Contains(nameof(Tag.Slug));
            if (!isUniqueSlugConstraint)
            {
                throw;
            }

            throw new SlugConflictException(newTag.Slug, e);
        }
    }

    public async Task UpdateTag(Tag tagToUpdate, CancellationToken ct = default)
    {
        try
        {
            _context.Tags.Update(tagToUpdate);
            await _context.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException e)
        {
            bool isUniqueSlugConstraint = e.ConstraintProperties.Contains(nameof(Tag.Slug));
            if (!isUniqueSlugConstraint)
            {
                throw;
            }

            throw new SlugConflictException(tagToUpdate.Slug, e);
        }
    }

    public async Task DeleteTag(Tag tagToDelete, CancellationToken ct = default)
    {
        _context.Tags.Remove(tagToDelete);
        await _context.SaveChangesAsync(ct);
    }
}