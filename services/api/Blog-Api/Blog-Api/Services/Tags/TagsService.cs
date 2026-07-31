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

    public async Task<Tag?> GetTag(Guid id)
    {
        return await _tagsRepository.GetTagById(id);
    }

    public async Task<Tag?> GetTag(string slug)
    {
        return await _tagsRepository.GetTagBySlug(slug);
    }

    public async Task<List<Tag>> GetAllTags()
    {
        return await _tagsRepository.GetAllTags();
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids)
    {
        return await _tagsRepository.GetAllTagsById(ids);
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<string> slugs)
    {
        return await _tagsRepository.GetAllTagsBySlug(slugs);
    }

    public async Task<List<Tag>> GetAllTags(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<string> slugs)
    {
        return await _tagsRepository.GetAllTagsByIdOrSlug(ids, slugs);
    }

    public async Task DeleteTag(Tag tag)
    {
        await _tagsRepository.DeleteTag(tag);
    }

    public async Task UpdateTag(Tag tag, string? name, string? slug)
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

        await _tagsRepository.UpdateTag(tag);
    }

    public async Task<Tag> CreateTag(Tag tag)
    {
        await _tagsRepository.AddTag(tag);
        return tag;
    }
}