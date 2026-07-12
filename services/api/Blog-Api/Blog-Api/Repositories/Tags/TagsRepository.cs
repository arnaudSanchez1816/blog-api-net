using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.Tags;

public class TagsRepository : ITagsRepository
{
    private readonly DataContext _context;

    public TagsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Tag>> GetAllTags()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task<Tag?> GetTagBySlug(string slug)
    {
        return await _context.Tags.SingleOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<Tag?> GetTagById(Guid id)
    {
        return await _context.Tags.SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Tag>> GetAllTagsBySlug(IReadOnlyCollection<string> slugs)
    {
        return await _context.Tags.Where(x => slugs.Contains(x.Slug)).ToListAsync();
    }

    public async Task<List<Tag>> GetAllTagsById(IReadOnlyCollection<Guid> ids)
    {
        return await _context.Tags.Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    public async Task<bool> AddTag(Tag newTag)
    {
        _context.Tags.Add(newTag);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateTag(Tag tagToUpdate)
    {
        _context.Tags.Update(tagToUpdate);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteTag(Tag tagToDelete)
    {
        _context.Tags.Remove(tagToDelete);
        return await _context.SaveChangesAsync() > 0;
    }
}