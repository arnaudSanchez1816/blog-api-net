using BlogApi.Domain;

namespace BlogApi.Repositories.Tags;

public interface ITagsRepository
{
    public Task<Tag?> GetTagBySlug(string slug);
    public Task<Tag?> GetTagById(Guid id);
    public Task<List<Tag>> GetAllTags();
    public Task<List<Tag>> GetAllTagsBySlug(IReadOnlyCollection<string> slugs);
    public Task<List<Tag>> GetAllTagsById(IReadOnlyCollection<Guid> ids);

    public Task<List<Tag>> GetAllTagsByIdOrSlug(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> slugs);

    public Task AddTag(Tag newTag);
    public Task UpdateTag(Tag tagToUpdate);
    public Task DeleteTag(Tag tagToDelete);
}