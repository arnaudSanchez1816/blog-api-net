using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class TagsRepositoryTests : IntegrationTestBase
{
    private ITagsRepository _tagsRepository = null!;

    public TagsRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override Task OnInitializeAsync()
    {
        _tagsRepository = GetRequiredService<ITagsRepository>();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateTag_UpdatesTag_WhenTagExists()
    {
        // Arrange
        Tag tag = new Tag
        {
            Name = "Tag name",
            Slug = "tag-slug"
        };
        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);

        // Act
        tag.Name = "Updated tag name";
        tag.Slug = "updated-slug";
        await _tagsRepository.UpdateTag(tag, TestContext.Current.CancellationToken);

        // Assert
        Tag? foundTag = await _tagsRepository.GetTagBySlug("updated-slug", TestContext.Current.CancellationToken);
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("Updated tag name");
        foundTag.Slug.Should().Be("updated-slug");
        foundTag.Id.Should().Be(tag.Id);
        Tag? oldTag = await _tagsRepository.GetTagBySlug("tag-slug", TestContext.Current.CancellationToken);
        oldTag.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTag_BySlug_DoesNotAffectOtherTags()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        await _tagsRepository.AddTag(tag1, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag2, TestContext.Current.CancellationToken);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagBySlug("dotnet", TestContext.Current.CancellationToken);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming";
        tagToUpdate.Slug = "csharp-programming";
        await _tagsRepository.UpdateTag(tagToUpdate, TestContext.Current.CancellationToken);

        // Assert
        Tag? aspTag = await _tagsRepository.GetTagBySlug("asp", TestContext.Current.CancellationToken);
        aspTag.Should().NotBeNull();
        aspTag.Name.Should().Be("Asp Net core");
        aspTag.Slug.Should().Be("asp");
    }

    [Fact]
    public async Task UpdateTag_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        Tag nonExistentTag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = "Dotnet Programming",
            Slug = "dotnet-programming"
        };

        // Act
        Func<Task> act = async () => await _tagsRepository.UpdateTag(nonExistentTag);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UpdateTag_ById_UpdatesTag_WhenTagExists()
    {
        // Arrange
        Tag tag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);
        Guid tagId = tag.Id;

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tagId, TestContext.Current.CancellationToken);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "Dotnet Programming";
        tagToUpdate.Slug = "dotnet-programming";
        await _tagsRepository.UpdateTag(tagToUpdate, TestContext.Current.CancellationToken);

        // Assert
        Tag? foundTag = await _tagsRepository.GetTagById(tagId, TestContext.Current.CancellationToken);
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("Dotnet Programming");
        foundTag.Slug.Should().Be("dotnet-programming");
    }

    [Fact]
    public async Task UpdateTag_ById_DoesNotAffectOtherTags()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        await _tagsRepository.AddTag(tag1, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag2, TestContext.Current.CancellationToken);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tag1.Id, TestContext.Current.CancellationToken);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming";
        tagToUpdate.Slug = "csharp-programming";
        await _tagsRepository.UpdateTag(tagToUpdate, TestContext.Current.CancellationToken);

        // Assert
        Tag? aspTag = await _tagsRepository.GetTagBySlug("asp", TestContext.Current.CancellationToken);
        aspTag.Should().NotBeNull();
        aspTag.Name.Should().Be("Asp Net core");
        aspTag.Slug.Should().Be("asp");
    }

    [Fact]
    public async Task UpdateTag_BySlug_UpdatesOnlyName_WhenSlugRemainsTheSame()
    {
        // Arrange
        Tag tag = new Tag { Name = "C#", Slug = "csharp" };
        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagBySlug("csharp", TestContext.Current.CancellationToken);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming Language";
        await _tagsRepository.UpdateTag(tagToUpdate, TestContext.Current.CancellationToken);

        // Assert
        Tag? foundTag = await _tagsRepository.GetTagBySlug("csharp", TestContext.Current.CancellationToken);
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("C# Programming Language");
        foundTag.Slug.Should().Be("csharp");
    }

    [Fact]
    public async Task UpdateTag_ById_UpdatesOnlyName_WhenSlugRemainsTheSame()
    {
        // Arrange
        Tag tag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);
        Guid tagId = tag.Id;

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tagId, TestContext.Current.CancellationToken);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "Dotnet Programming Language";
        await _tagsRepository.UpdateTag(tagToUpdate, TestContext.Current.CancellationToken);

        // Assert
        Tag? foundTag = await _tagsRepository.GetTagById(tagId, TestContext.Current.CancellationToken);
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("Dotnet Programming Language");
        foundTag.Slug.Should().Be("dotnet");
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsMatchingTags_ExcludesNonMatching()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(tag1, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag2, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag3, TestContext.Current.CancellationToken);

        // Act
        List<Tag> result =
            await _tagsRepository.GetAllTagsById([tag1.Id, tag2.Id], TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == tag1.Id);
        result.Should().Contain(t => t.Id == tag2.Id);
        result.Should().NotContain(t => t.Id == tag3.Id);
    }

    [Fact]
    public async Task GetAllTagsBySlug_ReturnsMatchingTags_ExcludesNonMatching()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(tag1, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag2, TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(tag3, TestContext.Current.CancellationToken);

        // Act
        List<Tag> result =
            await _tagsRepository.GetAllTagsBySlug(["dotnet", "asp"], TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Slug == "dotnet");
        result.Should().Contain(t => t.Slug == "asp");
        result.Should().NotContain(t => t.Slug == "docker");
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsEmpty_WhenNoMatchesFound()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result =
            await _tagsRepository.GetAllTagsById([Guid.NewGuid(), Guid.NewGuid()],
                TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsBySlug_ReturnsEmpty_WhenNoMatchesFound()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsBySlug(["non-existent", "also-non-existent"],
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsEmpty_WhenInputCollectionIsEmpty()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsById([], TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsBySlug_ReturnsEmpty_WhenInputCollectionIsEmpty()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsBySlug([], TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsEmpty_WhenThereIsNoTags()
    {
        List<Tag> result =
            await _tagsRepository.GetAllTagsById([Guid.NewGuid()], TestContext.Current.CancellationToken);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsByIdOrSlug_ReturnsEmpty_WhenInputCollectionIsEmpty()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsByIdOrSlug([], [], TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsByIdOrSlug_ReturnsEmpty_WhenNoMatchFound()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" },
            TestContext.Current.CancellationToken);
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" },
            TestContext.Current.CancellationToken);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsByIdOrSlug([Guid.NewGuid()],
            ["random-slug"],
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsByIdOrSlug_ReturnsMatchingTags_ExcludesNonMatching()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag1, TestContext.Current.CancellationToken);
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        await _tagsRepository.AddTag(tag2, TestContext.Current.CancellationToken);
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(tag3, TestContext.Current.CancellationToken);

        // Act
        List<Tag> result =
            await _tagsRepository.GetAllTagsByIdOrSlug([tag1.Id], [tag2.Slug], TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Slug == "dotnet");
        result.Should().Contain(t => t.Slug == "asp");
        result.Should().NotContain(t => t.Slug == "docker");
    }

    [Fact]
    public async Task DeleteTag_Success_WhenTagExists()
    {
        Tag tag = new Tag { Name = "Tag to delete", Slug = "tag-to-delete" };
        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);

        await _tagsRepository.DeleteTag(tag, TestContext.Current.CancellationToken);

        Tag? tagDeleted = await _tagsRepository.GetTagBySlug(tag.Slug, TestContext.Current.CancellationToken);
        tagDeleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTag_Fail_WhenTagDoNotExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag", Id = Guid.NewGuid() };

        Func<Task> act = async () => await _tagsRepository.DeleteTag(tag, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddTag_Success_WhenTagDoNotExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag" };

        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);

        Tag? tagAdded = await _tagsRepository.GetTagBySlug(tag.Slug, TestContext.Current.CancellationToken);
        tagAdded.Should().NotBeNull();
        tagAdded.Name.Should().Be(tag.Name);
        tagAdded.Slug.Should().Be(tag.Slug);
    }

    [Fact]
    public async Task AddTag_Fail_WhenTagWithSameSlugExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag" };

        await _tagsRepository.AddTag(tag, TestContext.Current.CancellationToken);

        Tag newTag = new Tag { Name = "Duplicate tag", Slug = "tag" };

        Func<Task> act = async () => await _tagsRepository.AddTag(newTag, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetAllTags_Throws_WhenCancellationIsRequested()
    {
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _tagsRepository.GetAllTags(cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddTag_Throws_WhenCancellationIsRequested()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag" };
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _tagsRepository.AddTag(tag, cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}