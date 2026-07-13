using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class TagsRepositoryTests : IntegrationTestBase
{
    public TagsRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task TagsRepository_GetAllTagsById_ReturnsEmptyWhenThereIsNoTags()
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        ITagsRepository tagsRepository = scope.ServiceProvider.GetRequiredService<ITagsRepository>();

        List<Tag> result = await tagsRepository.GetAllTagsById([Guid.NewGuid()]);
        result.Should().BeEmpty();
    }
}