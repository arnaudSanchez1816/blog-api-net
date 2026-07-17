using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;
using BlogApi.Services.Tags;
using Moq;

namespace BlogApi.Unit.Services;

public class TagsServiceTests : IDisposable
{
    private readonly Mock<ITagsRepository> _tagsRepository;
    private readonly ITagsService _tagsService;

    public TagsServiceTests()
    {
        _tagsRepository = new Mock<ITagsRepository>();
        _tagsService = new TagsService(_tagsRepository.Object);
    }

    public void Dispose()
    {
        _tagsRepository.Reset();
    }

    [Fact]
    public async Task CreateTag_ReturnsTag_WhenValid()
    {
        const string name = "Tag name";
        const string slug = "tag-slug";

        Tag newTag = new Tag
        {
            Name = name,
            Slug = slug
        };
        Tag createdTag = await _tagsService.CreateTag(newTag);

        createdTag.Name.Should().Be(name);
        createdTag.Slug.Should().Be(slug);
        _tagsRepository.Verify(x => x.AddTag(newTag), Times.Once);
    }
}