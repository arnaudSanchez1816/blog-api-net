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
        Tag createdTag = await _tagsService.CreateTag(newTag, TestContext.Current.CancellationToken);

        createdTag.Name.Should().Be(name);
        createdTag.Slug.Should().Be(slug);
        _tagsRepository.Verify(x => x.AddTag(newTag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComment_UpdatesName_WhenNameProvided()
    {
        const string originalName = "Original name";
        const string originalSlug = "original-slug";
        Tag tag = new Tag
        {
            Name = originalName,
            Slug = originalSlug
        };

        await _tagsService.UpdateTag(tag, null, "new-slug", TestContext.Current.CancellationToken);

        tag.Name.Should().Be(originalName);
        tag.Slug.Should().Be("new-slug");
        _tagsRepository.Verify(x => x.UpdateTag(tag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTag_UpdatesName_WhenNameProvided()
    {
        const string originalName = "Original name";
        const string originalSlug = "original-slug";
        Tag tag = new Tag
        {
            Name = originalName,
            Slug = originalSlug
        };

        await _tagsService.UpdateTag(tag, "New name", null, TestContext.Current.CancellationToken);

        tag.Name.Should().Be("New name");
        tag.Slug.Should().Be(originalSlug);
        _tagsRepository.Verify(x => x.UpdateTag(tag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTag_UpdatesNameAndSlug_WhenBothProvided()
    {
        const string originalName = "Original name";
        const string originalSlug = "original-slug";
        Tag tag = new Tag
        {
            Name = originalName,
            Slug = originalSlug
        };

        await _tagsService.UpdateTag(tag, "New name", "new-slug", TestContext.Current.CancellationToken);

        tag.Name.Should().Be("New name");
        tag.Slug.Should().Be("new-slug");
        _tagsRepository.Verify(x => x.UpdateTag(tag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTag_SkipsRepositoryCall_WhenNeitherProvided()
    {
        const string originalName = "Original name";
        const string originalSlug = "original-slug";
        Tag tag = new Tag
        {
            Name = originalName,
            Slug = originalSlug
        };

        await _tagsService.UpdateTag(tag, null, null, TestContext.Current.CancellationToken);

        tag.Name.Should().Be(originalName);
        tag.Slug.Should().Be(originalSlug);
        _tagsRepository.Verify(x => x.UpdateTag(tag, It.IsAny<CancellationToken>()), Times.Never);
    }
}