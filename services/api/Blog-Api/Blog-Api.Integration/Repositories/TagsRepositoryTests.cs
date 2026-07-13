using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;

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
        bool result = await _tagsRepository.AddTag(tag);
        result.Should().BeTrue();

        // Act
        tag.Name = "Updated tag name";
        tag.Slug = "updated-slug";
        result = await _tagsRepository.UpdateTag(tag);

        // Assert
        result.Should().BeTrue();
        Tag? foundTag = await _tagsRepository.GetTagBySlug("updated-slug");
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("Updated tag name");
        foundTag.Slug.Should().Be("updated-slug");
        foundTag.Id.Should().Be(tag.Id);
        Tag? oldTag = await _tagsRepository.GetTagBySlug("tag-slug");
        oldTag.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTag_BySlug_DoesNotAffectOtherTags()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagBySlug("dotnet");
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming";
        tagToUpdate.Slug = "csharp-programming";
        bool result = await _tagsRepository.UpdateTag(tagToUpdate);

        // Assert
        result.Should().BeTrue();
        Tag? aspTag = await _tagsRepository.GetTagBySlug("asp");
        aspTag.Should().NotBeNull();
        aspTag.Name.Should().Be("Asp Net core");
        aspTag.Slug.Should().Be("asp");
    }

    [Fact]
    public async Task UpdateTag_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        Tag nonExistentTag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = "Dotnet Programming",
            Slug = "dotnet-programming"
        };

        // Act
        bool result = await _tagsRepository.UpdateTag(nonExistentTag);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTag_ById_UpdatesTag_WhenTagExists()
    {
        // Arrange
        Tag tag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag);
        Guid tagId = tag.Id;

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tagId);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "Dotnet Programming";
        tagToUpdate.Slug = "dotnet-programming";
        bool result = await _tagsRepository.UpdateTag(tagToUpdate);

        // Assert
        result.Should().BeTrue();
        Tag? foundTag = await _tagsRepository.GetTagById(tagId);
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
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tag1.Id);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming";
        tagToUpdate.Slug = "csharp-programming";
        bool result = await _tagsRepository.UpdateTag(tagToUpdate);

        // Assert
        result.Should().BeTrue();
        Tag? aspTag = await _tagsRepository.GetTagBySlug("asp");
        aspTag.Should().NotBeNull();
        aspTag.Name.Should().Be("Asp Net core");
        aspTag.Slug.Should().Be("asp");
    }

    [Fact]
    public async Task UpdateTag_BySlug_UpdatesOnlyName_WhenSlugRemainsTheSame()
    {
        // Arrange
        Tag tag = new Tag { Name = "C#", Slug = "csharp" };
        await _tagsRepository.AddTag(tag);

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagBySlug("csharp");
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "C# Programming Language";
        bool result = await _tagsRepository.UpdateTag(tagToUpdate);

        // Assert
        result.Should().BeTrue();
        Tag? foundTag = await _tagsRepository.GetTagBySlug("csharp");
        foundTag.Should().NotBeNull();
        foundTag.Name.Should().Be("C# Programming Language");
        foundTag.Slug.Should().Be("csharp");
    }

    [Fact]
    public async Task UpdateTag_ById_UpdatesOnlyName_WhenSlugRemainsTheSame()
    {
        // Arrange
        Tag tag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag);
        Guid tagId = tag.Id;

        // Act
        Tag? tagToUpdate = await _tagsRepository.GetTagById(tagId);
        tagToUpdate.Should().NotBeNull();
        tagToUpdate.Name = "Dotnet Programming Language";
        bool result = await _tagsRepository.UpdateTag(tagToUpdate);

        // Assert
        result.Should().BeTrue();
        Tag? foundTag = await _tagsRepository.GetTagById(tagId);
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
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);
        await _tagsRepository.AddTag(tag3);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsById([tag1.Id, tag2.Id]);

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
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);
        await _tagsRepository.AddTag(tag3);

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsBySlug(["dotnet", "asp"]);

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
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" });
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" });

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsById([Guid.NewGuid(), Guid.NewGuid()]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsBySlug_ReturnsEmpty_WhenNoMatchesFound()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" });
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" });

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsBySlug(["non-existent", "also-non-existent"]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsEmpty_WhenInputCollectionIsEmpty()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" });
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" });

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsById([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsBySlug_ReturnsEmpty_WhenInputCollectionIsEmpty()
    {
        // Arrange
        await _tagsRepository.AddTag(new Tag { Name = "Dotnet", Slug = "dotnet" });
        await _tagsRepository.AddTag(new Tag { Name = "Asp Net core", Slug = "asp" });

        // Act
        List<Tag> result = await _tagsRepository.GetAllTagsBySlug([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTagsById_ReturnsEmpty_WhenThereIsNoTags()
    {
        List<Tag> result = await _tagsRepository.GetAllTagsById([Guid.NewGuid()]);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTag_Success_WhenTagExists()
    {
        Tag tag = new Tag { Name = "Tag to delete", Slug = "tag-to-delete" };
        await _tagsRepository.AddTag(tag);

        bool deleted = await _tagsRepository.DeleteTag(tag);

        deleted.Should().BeTrue();
        Tag? tagDeleted = await _tagsRepository.GetTagBySlug(tag.Slug);
        tagDeleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTag_Fail_WhenTagDoNotExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag", Id = Guid.NewGuid() };

        bool deleted = await _tagsRepository.DeleteTag(tag);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task AddTag_Success_WhenTagDoNotExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag" };

        bool added = await _tagsRepository.AddTag(tag);

        added.Should().BeTrue();
        Tag? tagAdded = await _tagsRepository.GetTagBySlug(tag.Slug);
        tagAdded.Should().NotBeNull();
        tagAdded.Name.Should().Be(tag.Name);
        tagAdded.Slug.Should().Be(tag.Slug);
    }

    [Fact]
    public async Task AddTag_Fail_WhenTagWithSameSlugExists()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag" };

        await _tagsRepository.AddTag(tag);

        Tag newTag = new Tag { Name = "Duplicate tag", Slug = "tag" };

        bool added = await _tagsRepository.AddTag(newTag);

        added.Should().BeFalse();
    }
}