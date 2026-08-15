using AwesomeAssertions;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.Comments;
using BlogApi.Repositories.Posts;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class PostsRepositoryTests : IntegrationTestBase
{
    private BlogUser _author = null!;
    private ICommentsRepository _commentsRepository = null!;
    private DataContext _context = null!;
    private IPostsRepository _postsRepository = null!;

    public PostsRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _postsRepository = GetRequiredService<IPostsRepository>();
        _commentsRepository = GetRequiredService<ICommentsRepository>();
        _context = GetRequiredService<DataContext>();

        _author = await CreateUser("author@example.com", "Author name");
    }

    private async Task<BlogUser> CreateUser(string email, string name)
    {
        BlogUser user = new BlogUser
        {
            UserName = email,
            Email = email,
            DisplayName = name
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task AddCommentsToPost(Guid postId, int count)
    {
        for (int i = 0; i < count; i++)
            await _commentsRepository.AddComment(new Comment
            {
                Username = $"commenter-{i}",
                Body = "Comment body",
                CreatedAt = DateTimeOffset.UtcNow,
                PostId = postId
            });
    }

    #region GetPostBySlugWithComments

    [Fact]
    public async Task GetPostBySlugWithComments_ReturnsPostWithNoComments_WhenPostHasNoComments()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments-loaded",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithComments(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Comments.Should().HaveCount(0);
    }

    [Fact]
    public async Task GetPostBySlugWithComments_ReturnsPostWithComments_WithCommentsSortedInAscendingCreationDate()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments-loaded",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);
        Comment firstComment = new Comment
        {
            Body = "comment b",
            Username = "C author",
            PostId = post.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };
        await _commentsRepository.AddComment(firstComment, ct: TestContext.Current.CancellationToken);
        Comment secondComment = new Comment
        {
            Body = "comment body 2",
            Username = "Author",
            PostId = post.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };
        await _commentsRepository.AddComment(secondComment, ct: TestContext.Current.CancellationToken);
        Comment lastComment = new Comment
        {
            Body = "comment body 3",
            Username = "Author",
            PostId = post.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _commentsRepository.AddComment(lastComment, ct: TestContext.Current.CancellationToken);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithComments(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Comments.Should().HaveCount(3);
        foundPost.Comments.Should().ContainInConsecutiveOrder(firstComment, secondComment, lastComment);
    }


    [Fact]
    public async Task GetPostBySlugWithComments_ReturnsCorrectCommentsCount_WhenPostHasComments()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments-loaded",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);
        await AddCommentsToPost(post.Id, 4);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithComments(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.CommentsCount.Should().Be(4);
        foundPost.Comments.Should().HaveCount(4);
    }

    #endregion


    #region AddPost

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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? addedPost = await _postsRepository.GetPostBySlug(post.Slug, ct: TestContext.Current.CancellationToken);
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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

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

    #endregion

    #region DeletePost

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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        await _postsRepository.DeletePost(post, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? deletedPost = await _postsRepository.GetPostBySlug(post.Slug, ct: TestContext.Current.CancellationToken);
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

    #endregion

    #region UpdatePost

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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        post.Title = "Updated title";
        post.Body = "Updated body";
        await _postsRepository.UpdatePost(post, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlug(post.Slug, ct: TestContext.Current.CancellationToken);
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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken))!;
        trackedPost.Tags.Clear();
        trackedPost.Tags.Add(tag3);
        await _postsRepository.UpdatePost(trackedPost, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken);
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
        await _postsRepository.AddPost(postToUpdate, ct: TestContext.Current.CancellationToken);

        Post otherPost = new Post
        {
            Title = "Other post",
            Slug = "other-post-with-tags",
            AuthorId = _author.Id
        };
        otherPost.Tags.Add(sharedTag);
        await _postsRepository.AddPost(otherPost, ct: TestContext.Current.CancellationToken);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(postToUpdate.Slug, ct: TestContext.Current.CancellationToken))!;
        trackedPost.Tags.Clear();
        trackedPost.Tags.Add(sharedTag);
        await _postsRepository.UpdatePost(trackedPost, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? refetchedOtherPost = await _postsRepository.GetPostBySlugWithTags(otherPost.Slug, ct: TestContext.Current.CancellationToken);
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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post trackedPost = (await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken))!;
        trackedPost.Tags.Clear();
        await _postsRepository.UpdatePost(trackedPost, ct: TestContext.Current.CancellationToken);

        // Assert
        Post? updatedPost = await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken);
        updatedPost.Should().NotBeNull();
        updatedPost.Tags.Should().BeEmpty();
    }

    #endregion

    #region GetPostBySlug

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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlug(post.Slug, ct: TestContext.Current.CancellationToken);

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
        Post? foundPost = await _postsRepository.GetPostBySlug("non-existent-slug", ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().BeNull();
    }

    [Fact]
    public async Task GetPostBySlug_ReturnsCorrectCommentsCount_WhenPostHasComments()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);
        await AddCommentsToPost(post.Id, 3);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlug(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.CommentsCount.Should().Be(3);
    }

    #endregion

    #region GetPostBySlugWithTags

    [Fact]
    public async Task GetPostBySlugWithTags_ReturnsCorrectCommentsCount_WhenPostHasComments()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments-and-tags",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);
        await AddCommentsToPost(post.Id, 2);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.CommentsCount.Should().Be(2);
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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken);

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
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags(post.Slug, ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().NotBeNull();
        foundPost.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostBySlugWithTags_ReturnsNull_WhenPostDoesNotExist()
    {
        // Act
        Post? foundPost = await _postsRepository.GetPostBySlugWithTags("non-existent-slug", ct: TestContext.Current.CancellationToken);

        // Assert
        foundPost.Should().BeNull();
    }

    #endregion


    #region GetPosts

    [Fact]
    public async Task GetPosts_ReturnsMatchingPost_WhenQMatchesTitleCaseInsensitivePartial()
    {
        // Arrange
        Post matching = new Post
        {
            Title = "Introduction to Dotnet",
            Slug = "introduction-to-dotnet",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post nonMatching = new Post
        {
            Title = "Cooking recipes",
            Slug = "cooking-recipes",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        await _postsRepository.AddPost(matching, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(nonMatching, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Q = "dotNET" };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "introduction-to-dotnet");
    }

    [Fact]
    public async Task GetPosts_ReturnsMultiplePosts_WhenMultipleTitlesMatchQ()
    {
        // Arrange
        Post first = new Post
        {
            Title = "Dotnet basics",
            Slug = "dotnet-basics",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Advanced Dotnet",
            Slug = "advanced-dotnet",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        Post nonMatching = new Post
        {
            Title = "Cooking recipes",
            Slug = "cooking-recipes",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(nonMatching, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Q = "dotnet" };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "dotnet-basics");
        result.Should().Contain(p => p.Slug == "advanced-dotnet");
    }

    [Fact]
    public async Task GetPosts_ReturnsEmpty_WhenNoTitlesMatchQ()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Dotnet basics",
            Slug = "dotnet-basics",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Q = "nonexistent" };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenQIsEmptyString()
    {
        // Arrange
        Post first = new Post
        {
            Title = "Dotnet basics",
            Slug = "dotnet-basics",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Cooking recipes",
            Slug = "cooking-recipes",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Q = "" };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenQIsWhitespaceOnly()
    {
        // Arrange: whitespace-only Q is caught by string.IsNullOrWhiteSpace, so it is a no-op.
        Post post1 = new Post
        {
            Title = "Dot net core",
            Slug = "dot-net-core",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post post2 = new Post
        {
            Title = "Dot net core basics",
            Slug = "dot-net-core-basics",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(post1, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(post2, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Q = "   " };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllMatchingPosts_WhenGivenAuthorId()
    {
        // Arrange
        BlogUser author1 = _author;
        BlogUser author2 = await CreateUser("author2@email.com", "Author2 name");
        Post postWithAuthor1 = new Post
        {
            Title = "Dotnet post",
            Slug = "dotnet-post",
            AuthorId = author1.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        Post postWithAuthor2 = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = author2.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(postWithAuthor1, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithAuthor2, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Author = author1.Id };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "dotnet-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenAuthorIdIsNull()
    {
        // Arrange
        BlogUser author1 = _author;
        BlogUser author2 = await CreateUser("author2@email.com", "Author2 name");
        Post postWithAuthor1 = new Post
        {
            Title = "Dotnet post",
            Slug = "dotnet-post",
            AuthorId = author1.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        Post postWithAuthor2 = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = author2.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(postWithAuthor1, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithAuthor2, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Author = null };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPosts_ReturnsOnlyPublishedPosts_WhenIncludeUnpublishedIsFalse()
    {
        // Arrange
        Post published = new Post
        {
            Title = "Published post",
            Slug = "published-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post draft = new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id,
            PublishedAt = null
        };
        await _postsRepository.AddPost(published, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(draft, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { IncludeUnpublished = false };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "published-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsPublishedAndDraftPosts_WhenIncludeUnpublishedIsTrue()
    {
        // Arrange
        Post published = new Post
        {
            Title = "Published post",
            Slug = "published-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post draft = new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id,
            PublishedAt = null
        };
        await _postsRepository.AddPost(published, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(draft, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { IncludeUnpublished = true };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "published-post");
        result.Should().Contain(p => p.Slug == "draft-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsEmpty_WhenOnlyDraftsExistAndIncludeUnpublishedIsFalse()
    {
        // Arrange
        Post draft = new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id,
            PublishedAt = null
        };
        await _postsRepository.AddPost(draft, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { IncludeUnpublished = false };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_ReturnsMatchingPost_WhenFilteringBySingleTagSlug()
    {
        // Arrange
        Tag dotnetTag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag dockerTag = new Tag { Name = "Docker", Slug = "docker" };
        _context.Tags.AddRange(dotnetTag, dockerTag);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post postWithDotnetTag = new Post
        {
            Title = "Dotnet post",
            Slug = "dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        postWithDotnetTag.Tags.Add(dotnetTag);

        Post postWithDockerTag = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        postWithDockerTag.Tags.Add(dockerTag);

        await _postsRepository.AddPost(postWithDotnetTag, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithDockerTag, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Tags = ["dotnet"] };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "dotnet-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_MatchingAnyOfMultipleTagSlugs()
    {
        // Arrange
        Tag dotnetTag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag dockerTag = new Tag { Name = "Docker", Slug = "docker" };
        Tag cookingTag = new Tag { Name = "Cooking", Slug = "cooking" };
        _context.Tags.AddRange(dotnetTag, dockerTag, cookingTag);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post postWithDotnetTag = new Post
        {
            Title = "Dotnet post",
            Slug = "dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        postWithDotnetTag.Tags.Add(dotnetTag);

        Post postWithDockerTag = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        postWithDockerTag.Tags.Add(dockerTag);

        Post postWithCookingTag = new Post
        {
            Title = "Cooking post",
            Slug = "cooking-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        postWithCookingTag.Tags.Add(cookingTag);

        await _postsRepository.AddPost(postWithDotnetTag, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithDockerTag, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithCookingTag, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Tags = ["dotnet", "docker"] };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "dotnet-post");
        result.Should().Contain(p => p.Slug == "docker-post");
        result.Should().NotContain(p => p.Slug == "cooking-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsEmpty_WhenNoPostHasGivenTagSlug()
    {
        // Arrange
        Tag dotnetTag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        _context.Tags.Add(dotnetTag);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post post = new Post
        {
            Title = "Dotnet post",
            Slug = "dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        post.Tags.Add(dotnetTag);
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Tags = ["nonexistent-tag"] };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenTagsIsNull()
    {
        // Arrange
        Post first = new Post
        {
            Title = "First post",
            Slug = "first-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Second post",
            Slug = "second-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Tags = null };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenTagsIsEmptyCollection()
    {
        // Arrange
        Post first = new Post
        {
            Title = "First post",
            Slug = "first-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Second post",
            Slug = "second-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { Tags = [] };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPosts_OrdersByIdAscending_WhenSortByIsIdAscending()
    {
        // Arrange
        Post first = new Post
        {
            Title = "First post",
            Slug = "first-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Second post",
            Slug = "second-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery
        {
            SortBy = PostSortOption.IdAscending,
            IncludeUnpublished = true
        };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(p => p.Id);
    }

    [Fact]
    public async Task GetPosts_OrdersByIdDescending_WhenSortByIsIdDescending()
    {
        // Arrange
        Post first = new Post
        {
            Title = "First post",
            Slug = "first-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post second = new Post
        {
            Title = "Second post",
            Slug = "second-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(first, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(second, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery
        {
            SortBy = PostSortOption.IdDescending,
            IncludeUnpublished = true
        };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(p => p.Id);
    }

    [Fact]
    public async Task GetPosts_OrdersByPublishedAtAscending_WhenSortByIsPublishedAtAscending()
    {
        // Arrange
        DateTimeOffset sharedPublishedAt = DateTimeOffset.UtcNow.AddDays(-5);
        Post earliest = new Post
        {
            Title = "Earliest post",
            Slug = "earliest-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };
        Post tiedFirst = new Post
        {
            Title = "Tied first post",
            Slug = "tied-first-post",
            AuthorId = _author.Id,
            PublishedAt = sharedPublishedAt
        };
        Post tiedSecond = new Post
        {
            Title = "Tied second post",
            Slug = "tied-second-post",
            AuthorId = _author.Id,
            PublishedAt = sharedPublishedAt
        };
        await _postsRepository.AddPost(earliest, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(tiedFirst, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(tiedSecond, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { SortBy = PostSortOption.PublishedAtAscending };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(3);
        result[0].Slug.Should().Be("earliest-post");
        // Tied PublishedAt values should be tiebroken by Id ascending
        Guid firstTiedId = tiedFirst.Id.CompareTo(tiedSecond.Id) <= 0 ? tiedFirst.Id : tiedSecond.Id;
        result[1].Id.Should().Be(firstTiedId);
    }

    [Fact]
    public async Task GetPosts_OrdersByPublishedAtDescending_WhenSortByIsPublishedAtDescending()
    {
        // Arrange
        DateTimeOffset sharedPublishedAt = DateTimeOffset.UtcNow.AddDays(-5);
        Post latest = new Post
        {
            Title = "Latest post",
            Slug = "latest-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post tiedFirst = new Post
        {
            Title = "Tied first post",
            Slug = "tied-first-post",
            AuthorId = _author.Id,
            PublishedAt = sharedPublishedAt
        };
        Post tiedSecond = new Post
        {
            Title = "Tied second post",
            Slug = "tied-second-post",
            AuthorId = _author.Id,
            PublishedAt = sharedPublishedAt
        };
        await _postsRepository.AddPost(latest, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(tiedFirst, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(tiedSecond, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery { SortBy = PostSortOption.PublishedAtDescending };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(3);
        result[0].Slug.Should().Be("latest-post");
        // Tied PublishedAt values should still be tiebroken by Id ascending (ThenBy, not ThenByDescending)
        Guid firstTiedId = tiedFirst.Id.CompareTo(tiedSecond.Id) <= 0 ? tiedFirst.Id : tiedSecond.Id;
        result[1].Id.Should().Be(firstTiedId);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllMatchingPosts_WhenPaginationIsNull()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-i)
            }, ct: TestContext.Current.CancellationToken);

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(new GetPostsFilterQuery(), null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPosts_ReturnsPageSizeResultsAtCorrectOffset_WhenPaginated()
    {
        // Arrange: 5 posts ordered by PublishedAt descending (default sort), page 2 with page size 2
        // should skip the 2 most recent posts and return the next 2.
        for (int i = 0; i < 5; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-i)
            }, ct: TestContext.Current.CancellationToken);

        PaginationQuery pagination = new PaginationQuery(2, 2);

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(new GetPostsFilterQuery(), pagination, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "post-2");
        result.Should().Contain(p => p.Slug == "post-3");
        pagedResult.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPosts_ReturnsPartialResults_OnLastPage()
    {
        // Arrange: 5 posts, page size 2 means page 3 only has 1 remaining result
        for (int i = 0; i < 5; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-i)
            }, ct: TestContext.Current.CancellationToken);

        PaginationQuery pagination = new PaginationQuery(3, 2);

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(new GetPostsFilterQuery(), pagination, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "post-4");
        pagedResult.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPosts_ReturnsEmpty_WhenPageIsBeyondLastPage()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-i)
            }, ct: TestContext.Current.CancellationToken);

        PaginationQuery pagination = new PaginationQuery(3, 2);

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(new GetPostsFilterQuery(), pagination, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPosts_ReturnsAllPosts_WhenFilterIsNull()
    {
        // Arrange
        Post published = new Post
        {
            Title = "Published post",
            Slug = "published-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post draft = new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id,
            PublishedAt = null
        };
        await _postsRepository.AddPost(published, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(draft, ct: TestContext.Current.CancellationToken);

        // Act: null filter should default to excluding unpublished, no tag filter, default sort
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(null, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Slug == "published-post");
    }

    [Fact]
    public async Task GetPosts_CombinesTagFilterIncludeUnpublishedAndSortAndAuthor_Correctly()
    {
        // Arrange
        BlogUser author2 = await CreateUser("author2@email.com", "Author2 Name");
        Tag dotnetTag = new Tag { Name = "Dotnet", Slug = "dotnet" };
        Tag dockerTag = new Tag { Name = "Docker", Slug = "docker" };
        _context.Tags.AddRange(dotnetTag, dockerTag);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Post publishedDotnetOlder = new Post
        {
            Title = "Older published dotnet post",
            Slug = "older-published-dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-10),
            Tags = { dotnetTag }
        };

        Post publishedDotnetNewer = new Post
        {
            Title = "Newer published dotnet post",
            Slug = "newer-published-dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Tags = { dotnetTag }
        };

        Post author2DotnetPost = new Post
        {
            Title = "Author2 published dotnet post",
            Slug = "author2-published-dotnet-post",
            AuthorId = author2.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Tags = { dotnetTag }
        };

        Post draftDotnet = new Post
        {
            Title = "Draft dotnet post",
            Slug = "draft-dotnet-post",
            AuthorId = _author.Id,
            PublishedAt = null
        };
        draftDotnet.Tags.Add(dotnetTag);

        Post publishedDocker = new Post
        {
            Title = "Published docker post",
            Slug = "published-docker-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        publishedDocker.Tags.Add(dockerTag);

        await _postsRepository.AddPost(publishedDotnetOlder, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(publishedDotnetNewer, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(author2DotnetPost, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(draftDotnet, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(publishedDocker, ct: TestContext.Current.CancellationToken);

        GetPostsFilterQuery filter = new GetPostsFilterQuery
        {
            Tags = ["dotnet"],
            IncludeUnpublished = false,
            SortBy = PostSortOption.PublishedAtDescending,
            Author = _author.Id
        };

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(filter, null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result[0].Slug.Should().Be("newer-published-dotnet-post");
        result[1].Slug.Should().Be("older-published-dotnet-post");
    }

    [Fact]
    public async Task GetPosts_ReturnsCorrectCommentsCountPerPost_WhenPostsHaveDifferentCommentCounts()
    {
        // Arrange
        Post postWithTwoComments = new Post
        {
            Title = "Post with two comments",
            Slug = "post-with-two-comments",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        Post postWithNoComments = new Post
        {
            Title = "Post with no comments",
            Slug = "post-with-no-comments",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await _postsRepository.AddPost(postWithTwoComments, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(postWithNoComments, ct: TestContext.Current.CancellationToken);
        await AddCommentsToPost(postWithTwoComments.Id, 2);

        // Act
        PagedPostsResult pagedResult = await _postsRepository.GetPosts(new GetPostsFilterQuery(), null, ct: TestContext.Current.CancellationToken);
        List<Post> result = pagedResult.Posts;

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Slug == "post-with-two-comments" && p.CommentsCount == 2);
        result.Should().Contain(p => p.Slug == "post-with-no-comments" && p.CommentsCount == 0);
    }

    #endregion

    #region GetSlugsStartingWithSlug

    [Fact]
    public async Task GetSlugsStartingWithSlug_ReturnsExactMatchAndSuffixedMatches_ExcludesNonMatching()
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
        await _postsRepository.AddPost(exactMatch, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(suffixedMatch, ct: TestContext.Current.CancellationToken);
        await _postsRepository.AddPost(nonMatching, ct: TestContext.Current.CancellationToken);

        // Act
        IReadOnlyCollection<string> result = await _postsRepository.GetSlugsStartingWithSlug("post-title", ct: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p == "post-title");
        result.Should().Contain(p => p == "post-title-2");
        result.Should().NotContain(p => p == "other-post");
    }

    [Fact]
    public async Task GetSlugsStartingWithSlug_DoesNotMatchUnrelatedSlugWithSamePrefix()
    {
        // Arrange
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-title-extra",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post, ct: TestContext.Current.CancellationToken);

        // Act
        IReadOnlyCollection<string> result = await _postsRepository.GetSlugsStartingWithSlug("post-titl", ct: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSlugsStartingWithSlug_ReturnsEmpty_WhenNoMatchesFound()
    {
        // Arrange
        await _postsRepository.AddPost(new Post
        {
            Title = "Post title",
            Slug = "post-title",
            AuthorId = _author.Id
        }, ct: TestContext.Current.CancellationToken);

        // Act
        IReadOnlyCollection<string> result = await _postsRepository.GetSlugsStartingWithSlug("non-existent", ct: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    [Fact]
    public async Task GetPosts_Throws_WhenCancellationIsRequested()
    {
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _postsRepository.GetPosts(null, null, cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddPost_Throws_WhenCancellationIsRequested()
    {
        Post post = new Post
        {
            Title = "Post title",
            Slug = "post-title-cancelled",
            AuthorId = _author.Id
        };
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _postsRepository.AddPost(post, cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}