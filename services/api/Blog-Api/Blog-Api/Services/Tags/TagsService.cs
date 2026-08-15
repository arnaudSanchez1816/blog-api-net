using BlogApi.Domain;
using BlogApi.Repositories.Tags;

namespace BlogApi.Services.Tags;

public class TagsService : ITagsService
{
    private readonly ITagsRepository _tagsRepository;

    public TagsService(ITagsRepository tagsRepository)
    {
        _tagsRepository = tagsRepository;
    }

    public async Task<Tag?> GetTag(Guid id, CancellationToken ct = default)
    {
        return await _tagsRepository.GetTagById(id, ct);
    }

    public async Task<Tag?> GetTag(string slug, CancellationToken ct = default)
    {
        return await _tagsRepository.GetTagBySlug(slug, ct);
    }

    public async Task<List<Tag>> GetAllTags(CancellationToken ct = default)
    {
        return await _tagsRepository.GetAllTags(ct);
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        return await _tagsRepository.GetAllTagsById(ids, ct);
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<string> slugs, CancellationToken ct = default)
    {
        return await _tagsRepository.GetAllTagsBySlug(slugs, ct);
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> slugs,
        CancellationToken ct = default)
    {
        return await _tagsRepository.GetAllTagsByIdOrSlug(ids, slugs, ct);
    }

    public async Task DeleteTag(Tag tag, CancellationToken ct = default)
    {
        await _tagsRepository.DeleteTag(tag, ct);
    }

    public async Task UpdateTag(Tag tag, string? name, string? slug, CancellationToken ct = default)
    {
        if (name is null && slug is null)
        {
            return;
        }

        if (name is not null)
        {
            tag.Name = name;
        }

        if (slug is not null)
        {
            tag.Slug = slug;
        }

        await _tagsRepository.UpdateTag(tag, ct);
    }

    public async Task<Tag> CreateTag(Tag tag, CancellationToken ct = default)
    {
        await _tagsRepository.AddTag(tag, ct);
        return tag;
    }
}