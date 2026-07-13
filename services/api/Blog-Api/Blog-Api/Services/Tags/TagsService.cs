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

    public async Task<bool> AddTag(Tag tag)
    {
        return await _tagsRepository.AddTag(tag);
    }

    public async Task<bool> DeleteTag(Tag tag)
    {
        return await _tagsRepository.DeleteTag(tag);
    }

    public async Task<bool> UpdateTag(Tag tag)
    {
        return await _tagsRepository.UpdateTag(tag);
    }
}