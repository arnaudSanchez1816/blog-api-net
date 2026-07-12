using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class TagsRepositoryTests
{
    private readonly BlogApiFactory _factory;

    public TagsRepositoryTests(BlogApiFactory webAppFactory)
    {
        _factory = webAppFactory;
    }

    [Fact]
    public async Task TagsRepository_GetAllTagsById_ReturnsEmptyWhenThereIsNoTags()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ITagsRepository tagsRepository = scope.ServiceProvider.GetRequiredService<ITagsRepository>();

        List<Tag> result = await tagsRepository.GetAllTagsById([Guid.NewGuid()]);
        result.Should().BeEmpty();
    }
}