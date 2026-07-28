using AwesomeAssertions;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.Posts;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class PostsRepositoryTests : IntegrationTestBase
{
    private BlogUser _author = null!;
    private DataContext _context = null!;
    private IPostsRepository _postsRepository = null!;

    public PostsRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _postsRepository = GetRequiredService<IPostsRepository>();
        _context = GetRequiredService<DataContext>();

        _author = new BlogUser
        {
            UserName = "author@example.com",
            Email = "author@example.com",
            DisplayName = "Author Name"
        };
        _context.Users.Add(_author);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddPost_Success_WhenPostDoesNotExist()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };

        // Act
        await _postsRepository.AddPost(post);

        // Assert
        Post? addedPost = await _postsRepository.GetPostBySlug(post.Slug);
        addedPost.Should().NotBeNull();
        addedPost.Title.Should().Be(post.Title);
        addedPost.Slug.Should().Be(post.Slug);
        addedPost.AuthorId.Should().Be(_author.Id);
    }

    [Fact]
    public async Task AddPost_Fail_WhenPostWithSameSlugExists()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        Post duplicatePost = new Post
        {
            Title = "Duplicate post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };

        // Act
        Func<Task> act = async () => await _postsRepository.AddPost(duplicatePost);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeletePost_Success_WhenPostExists()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post to delete",
            Slug = "post-to-delete",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        // Act
        await _postsRepository.DeletePost(post);

        // Assert
        Post? deletedPost = await _postsRepository.GetPostBySlug(post.Slug);
        deletedPost.Should().BeNull();
    }

    [Fact]
    public async Task DeletePost_Fail_WhenPostDoesNotExist()
    {
        // Arrange
        Post post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Nonexistent post",
            Slug = "nonexistent-post",
            AuthorId = _author.Id
        };

        // Act
        Func<Task> act = async () => await _postsRepository.DeletePost(post);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UpdatePost_UpdatesScalarFields_WhenPostExists()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Original title",
            Slug = "original-slug",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        // Act
        post.Title = "Updated title";
        post.Body = "Updated body";
        await _postsRepository.UpdatePost(post);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlug(post.Slug);
        updatedPost.Should().NotBeNull();
        updatedPost.Title.Should().Be("Updated title");
        updatedPost.Body.Should().Be("Updated body");
    }

    [Fact]
    public async Task UpdatePost_Fail_WhenPostDoesNotExist()
    {
        // Arrange
        Post post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Nonexistent post",
            Slug = "nonexistent-update-post",
            AuthorId = _author.Id
        };

        // Act
        Func<Task> act = async () => await _postsRepository.UpdatePost(post);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UpdatePost_ReplacesTags_WhenTagsAreChanged()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        _context.Tags.AddRange(tag1, tag2, tag3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-with-tags",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag1);
        post.Tags.Add(tag2);
        await _postsRepository.AddPost(post);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(post.Slug))!;
        trackedPost.Tags.Clear();
        trackedPost.Tags.Add(tag3);
        await _postsRepository.UpdatePost(trackedPost);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlugWithTags(post.Slug);
        updatedPost.Should().NotBeNull();
        updatedPost.Tags.Should().HaveCount(1);
        updatedPost.Tags.Should().Contain(t => t.Slug == "docker");
        updatedPost.Tags.Should().NotContain(t => t.Slug == "dotnet");
        updatedPost.Tags.Should().NotContain(t => t.Slug == "asp");
    }

    [Fact]
    public async Task UpdatePost_ReplacesTags_DoesNotAffectOtherPostsTags()
    {
        // Arrange
        Tag sharedTag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tagToRemove = new Tag { Name = "Asp Net core", Slug = "asp" };
        _context.Tags.AddRange(sharedTag, tagToRemove);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post postToUpdate = new Post
        {
            Title = "Post to update",
            Slug = "post-to-update",
            AuthorId = _author.Id
        };
        postToUpdate.Tags.Add(sharedTag);
        postToUpdate.Tags.Add(tagToRemove);
        await _postsRepository.AddPost(postToUpdate);

        Post otherPost = new Post
        {
            Title = "Other post",
            Slug = "other-post-with-tags",
            AuthorId = _author.Id
        };
        otherPost.Tags.Add(sharedTag);
        await _postsRepository.AddPost(otherPost);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(postToUpdate.Slug))!;
        trackedPost.Tags.Clear();
        trackedPost.Tags.Add(sharedTag);
        await _postsRepository.UpdatePost(trackedPost);

        // Assert
        Post? refetchedOtherPost = await _postsRepository.GetPostBySlugWithTags(otherPost.Slug);
        refetchedOtherPost.Should().NotBeNull();
        refetchedOtherPost.Tags.Should().HaveCount(1);
        refetchedOtherPost.Tags.Should().Contain(t => t.Slug == "dotnet");
    }

    [Fact]
    public async Task UpdatePost_ClearsAllTags_WhenTagsCollectionIsEmptied()
    {
        // Arrange
        Tag tag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-to-clear-tags",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag);
        await _postsRepository.AddPost(post);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(post.Slug))!;
        trackedPost.Tags.Clear();
        await _postsRepository.UpdatePost(trackedPost);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlugWithTags(post.Slug);
        updatedPost.Should().NotBeNull();
        updatedPost.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostBySlug_ReturnsPost_WhenPostExists()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlug(post.Slug);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Id.Should().Be(post.Id);
        foundPost.Title.Should().Be(post.Title);
        foundPost.Author.Should().NotBeNull();
        foundPost.Author.Id.Should().Be(_author.Id);
    }

    [Fact]
    public async Task GetPostBySlug_ReturnsNull_WhenPostDoesNotExist()
    {
        // Act
        Post? foundPost = await _postsRepository.GetPostBySlug("non-existent-slug");

        // Assert
        foundPost.Should().BeNull();
    }

    [Fact]
    public async Task GetPostBySlugWithTags_ReturnsPostWithTags_WhenPostExists()
    {
        // Arrange
        Tag tag1 = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag tag2 = new Tag { Name = "Asp Net core", Slug = "asp" };
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag1);
        post.Tags.Add(tag2);
        await _postsRepository.AddPost(post);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags(post.Slug);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Tags.Should().HaveCount(2);
        foundPost.Tags.Should().Contain(t => t.Slug == "dotnet");
        foundPost.Tags.Should().Contain(t => t.Slug == "asp");
    }

    [Fact]
    public async Task GetPostBySlugWithTags_ReturnsPostWithEmptyTags_WhenPostHasNoTags()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-slug",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags(post.Slug);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostBySlugWithTags_ReturnsNull_WhenPostDoesNotExist()
    {
        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags("non-existent-slug");

        // Assert
        foundPost.Should().BeNull();
    }

    [Fact]
    public async Task GetPostsStartingWithSlug_ReturnsExactMatchAndSuffixedMatches_ExcludesNonMatching()
    {
        // Arrange
        Post exactMatch = new Post
        {
            Title = "Post title",
            Slug = "post-title",
            AuthorId = _author.Id
        };
        Post suffixedMatch = new Post
        {
            Title = "Post title",
            Slug = "post-title-2",
            AuthorId = _author.Id
        };
        Post nonMatching = new Post
        {
            Title = "Other post",
            Slug = "other-post",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(exactMatch);
        await _postsRepository.AddPost(suffixedMatch);
        await _postsRepository.AddPost(nonMatching);

        // Act
        IReadOnlyCollection<Post> result = await _postsRepository.GetPostsStartingWithSlug("post-title");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "post-title");
        result.Should().Contain(p => p.Slug == "post-title-2");
        result.Should().NotContain(p => p.Slug == "other-post");
    }

    [Fact]
    public async Task GetPostsStartingWithSlug_DoesNotMatchUnrelatedSlugWithSamePrefix()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-title-extra",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        // Act
        IReadOnlyCollection<Post> result = await _postsRepository.GetPostsStartingWithSlug("post-titl");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostsStartingWithSlug_ReturnsEmpty_WhenNoMatchesFound()
    {
        // Arrange
        await _postsRepository.AddPost(new Post
        {
            Title = "Post title",
            Slug = "post-title",
            AuthorId = _author.Id
        });

        // Act
        IReadOnlyCollection<Post> result = await _postsRepository.GetPostsStartingWithSlug("non-existent");

        // Assert
        result.Should().BeEmpty();
    }
}