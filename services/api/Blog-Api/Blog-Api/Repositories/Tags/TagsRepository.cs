using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.Tags;

public class TagsRepository : ITagsRepository
{
    private readonly DataContext _context;
    private readonly ILogger<TagsRepository> _logger;

    public TagsRepository(DataContext context, ILogger<TagsRepository> logger)
    {
        _context = context;
        _logger = logger;
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
        try
        {
            _context.Tags.Add(newTag);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add tag {tag}", newTag);
            return false;
        }
    }

    public async Task<bool> UpdateTag(Tag tagToUpdate)
    {
        try
        {
            _context.Tags.Update(tagToUpdate);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update tag {tag}", tagToUpdate);
            return false;
        }
    }

    public async Task<bool> DeleteTag(Tag tagToDelete)
    {
        try
        {
            _context.Tags.Remove(tagToDelete);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete tag {tag}", tagToDelete);
            return false;
        }
    }
}