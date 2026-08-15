using BlogApi.Domain;

namespace BlogApi.Repositories.Tags;

public interface ITagsRepository
{
    public Task<Tag?> GetTagBySlug(string slug, CancellationToken ct = default);
    public Task<Tag?> GetTagById(Guid id, CancellationToken ct = default);
    public Task<List<Tag>> GetAllTags(CancellationToken ct = default);
    public Task<List<Tag>> GetAllTagsBySlug(IReadOnlyCollection<string> slugs, CancellationToken ct = default);
    public Task<List<Tag>> GetAllTagsById(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    public Task<List<Tag>> GetAllTagsByIdOrSlug(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> slugs,
        CancellationToken ct = default);

    public Task AddTag(Tag newTag, CancellationToken ct = default);
    public Task UpdateTag(Tag tagToUpdate, CancellationToken ct = default);
    public Task DeleteTag(Tag tagToDelete, CancellationToken ct = default);
}