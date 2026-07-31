using BlogApi.Domain;

namespace BlogApi.Services.Tags;

public interface ITagsService
{
    public Task<Tag?> GetTag(Guid id);
    public Task<Tag?> GetTag(string slug);
    public Task<List<Tag>> GetAllTags();
    public Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids);
    public Task<List<Tag>> GetAllTags(IReadOnlyCollection<string> slugs);
    public Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> slugs);

    public Task<Tag> CreateTag(Tag tag);
    public Task DeleteTag(Tag tag);
    public Task UpdateTag(Tag tag, string? name, string? slug);
}