using BlogApi.Domain;

namespace BlogApi.Services.Tags;

public interface ITagsService
{
    public Task<Tag?> GetTag(Guid id, CancellationToken ct = default);
    public Task<Tag?> GetTag(string slug, CancellationToken ct = default);
    public Task<List<Tag>> GetAllTags(CancellationToken ct = default);
    public Task<List<Tag>> GetAllTags(IReadOnlyCollection<string> slugs, CancellationToken ct = default);

    public Task<Tag> CreateTag(Tag tag, CancellationToken ct = default);
    public Task DeleteTag(Tag tag, CancellationToken ct = default);
    public Task UpdateTag(Tag tag, string? name, string? slug, CancellationToken ct = default);
}