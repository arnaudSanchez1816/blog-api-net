using AwesomeAssertions;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Repositories.Posts;
using BlogApi.Services.Comments;
using BlogApi.Services.Markdown;
using BlogApi.Services.Posts;
using BlogApi.Services.Tags;
using BlogApi.Services.Text;
using BlogApi.Utils;
using Moq;

namespace BlogApi.Unit.Services;

public class PostsServiceTests : IDisposable
{
    private readonly Mock<ICommentsService> _commentsService;
    private readonly Mock<IMarkdownService> _markdownService;
    private readonly Mock<IPostsRepository> _postsRepository;
    private readonly PostsService _postsService;
    private readonly Mock<ITagsService> _tagsService;
    private readonly Mock<ITextService> _textService;

    public PostsServiceTests()
    {
        _postsRepository = new Mock<IPostsRepository>();
        _tagsService = new Mock<ITagsService>();
        _markdownService = new Mock<IMarkdownService>();
        _textService = new Mock<ITextService>();
        _commentsService = new Mock<ICommentsService>();
        _postsService = new PostsService(_postsRepository.Object, _tagsService.Object, _markdownService.Object,
            _textService.Object, _commentsService.Object);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(It.IsAny<string>()))
            .ReturnsAsync(new List<Post>());
    }

    public void Dispose()
    {
        _postsRepository.Reset();
        _tagsService.Reset();
        _markdownService.Reset();
        _textService.Reset();
    }

    private static Post CreatePost()
    {
        return new Post
        {
            Title = "Original title",
            Slug = "original-title",
            Body = "Original body",
            Description = "Original description",
            ReadingTime = 1,
            AuthorId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task UpdatePost_UpdatesBody_WhenBodyProvided()
    {
        Post post = CreatePost();
        const string markdownBody = "# Heading\n\nSome markdown body";
        const string plainText = "Heading Some markdown body";
        const string firstWords = "Heading Some";
        const int readingTime = 1;

        _markdownService.Setup(x => x.MarkdownToPlainText(markdownBody)).Returns(plainText);
        _textService.Setup(x => x.GetFirstWordsSubstring(plainText, 50)).Returns(firstWords);
        _textService.Setup(x => x.EstimateReadingTime(plainText)).Returns(readingTime);

        UpdatePostRequest request = new UpdatePostRequest { Body = markdownBody };

        await _postsService.UpdatePost(post, request);

        post.Body.Should().Be(markdownBody);
        post.Description.Should().Be(firstWords + "…");
        post.ReadingTime.Should().Be(readingTime);
        _markdownService.Verify(x => x.MarkdownToPlainText(markdownBody), Times.Once);
        _textService.Verify(x => x.GetFirstWordsSubstring(plainText, 50), Times.Once);
        _textService.Verify(x => x.EstimateReadingTime(plainText), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_LeavesBodyUnchanged_WhenBodyNotProvided()
    {
        Post post = CreatePost();
        string originalBody = post.Body;
        string originalDescription = post.Description;
        int originalReadingTime = post.ReadingTime;

        UpdatePostRequest request = new UpdatePostRequest { Body = null };

        await _postsService.UpdatePost(post, request);

        post.Body.Should().Be(originalBody);
        post.Description.Should().Be(originalDescription);
        post.ReadingTime.Should().Be(originalReadingTime);
        _markdownService.Verify(x => x.MarkdownToPlainText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_SetsPublishedAt_WhenIsPublishedIsTrueAndPostIsUnpublished()
    {
        Post post = CreatePost();
        post.PublishedAt = null;
        DateTimeOffset before = DateTimeOffset.UtcNow;

        UpdatePostRequest request = new UpdatePostRequest { IsPublished = true };

        await _postsService.UpdatePost(post, request);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        post.PublishedAt.Should().NotBeNull();
        post.PublishedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdatePost_ClearsPublishedAt_WhenIsPublishedIsFalseAndPostIsPublished()
    {
        Post post = CreatePost();
        post.PublishedAt = DateTimeOffset.UtcNow;

        UpdatePostRequest request = new UpdatePostRequest { IsPublished = false };

        await _postsService.UpdatePost(post, request);

        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePost_LeavesPublishedAtUnchanged_WhenIsPublishedIsTrueAndPostIsAlreadyPublished()
    {
        Post post = CreatePost();
        DateTimeOffset originalPublishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        post.PublishedAt = originalPublishedAt;

        UpdatePostRequest request = new UpdatePostRequest { IsPublished = true };

        await _postsService.UpdatePost(post, request);

        post.PublishedAt.Should().Be(originalPublishedAt);
    }

    [Fact]
    public async Task UpdatePost_LeavesPublishedAtUnchanged_WhenIsPublishedIsFalseAndPostIsAlreadyUnpublished()
    {
        Post post = CreatePost();
        post.PublishedAt = null;

        UpdatePostRequest request = new UpdatePostRequest { IsPublished = false };

        await _postsService.UpdatePost(post, request);

        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePost_LeavesPublishedAtUnchanged_WhenIsPublishedIsNotProvided()
    {
        Post post = CreatePost();
        DateTimeOffset originalPublishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        post.PublishedAt = originalPublishedAt;

        UpdatePostRequest request = new UpdatePostRequest { IsPublished = null };

        await _postsService.UpdatePost(post, request);

        post.PublishedAt.Should().Be(originalPublishedAt);
    }

    [Fact]
    public async Task UpdatePost_UpdatesTitleAndSlug_WhenTitleProvided()
    {
        Post post = CreatePost();
        const string title = "A Brand New Title";
        string expectedSlug = SlugGenerator.Generate(title);

        UpdatePostRequest request = new UpdatePostRequest { Title = title };

        await _postsService.UpdatePost(post, request);

        post.Title.Should().Be(title);
        post.Slug.Should().Be(expectedSlug);
    }

    [Fact]
    public async Task UpdatePost_LeavesTitleAndSlugUnchanged_WhenTitleNotProvided()
    {
        Post post = CreatePost();
        string originalTitle = post.Title;
        string originalSlug = post.Slug;

        UpdatePostRequest request = new UpdatePostRequest { Title = null };

        await _postsService.UpdatePost(post, request);

        post.Title.Should().Be(originalTitle);
        post.Slug.Should().Be(originalSlug);
    }

    [Fact]
    public async Task UpdatePost_AppendsSuffixToSlug_WhenBaseSlugAndFirstSuffixAreTaken()
    {
        Post post = CreatePost();
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(baseSlug)).ReturnsAsync(new List<Post>
        {
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-1",
                AuthorId = Guid.NewGuid()
            },
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-2",
                AuthorId = Guid.NewGuid()
            }
        });

        UpdatePostRequest request = new UpdatePostRequest { Title = title };

        await _postsService.UpdatePost(post, request);

        post.Slug.Should().Be($"{baseSlug}-3");
    }

    [Fact]
    public async Task UpdatePost_AppendsSuffixToSlug_WhenExactBaseSlugIsTaken()
    {
        Post post = CreatePost();
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(baseSlug)).ReturnsAsync(new List<Post>
        {
            new Post
            {
                Title = title,
                Slug = baseSlug,
                AuthorId = Guid.NewGuid()
            }
        });

        UpdatePostRequest request = new UpdatePostRequest { Title = title };

        await _postsService.UpdatePost(post, request);

        post.Slug.Should().Be($"{baseSlug}-2");
    }

    [Fact]
    public async Task UpdatePost_ReplacesTags_WhenTagsProvided()
    {
        Post post = CreatePost();
        post.Tags.Add(new Tag { Name = "Old tag", Slug = "old-tag" });

        List<Tag> newTags = new List<Tag>
        {
            new Tag { Name = "New tag 1", Slug = "new-tag-1" },
            new Tag { Name = "New tag 2", Slug = "new-tag-2" }
        };
        List<string> tagSlugs = new List<string> { "new-tag-1", "new-tag-2" };

        _tagsService.Setup(x => x.GetAllTags(tagSlugs)).ReturnsAsync(newTags);

        UpdatePostRequest request = new UpdatePostRequest { Tags = tagSlugs };

        await _postsService.UpdatePost(post, request);

        post.Tags.Should().BeEquivalentTo(newTags);
    }

    [Fact]
    public async Task UpdatePost_LeavesTagsUnchanged_WhenTagsNotProvided()
    {
        Post post = CreatePost();
        Tag originalTag = new Tag { Name = "Old tag", Slug = "old-tag" };
        post.Tags.Add(originalTag);

        UpdatePostRequest request = new UpdatePostRequest { Tags = null };

        await _postsService.UpdatePost(post, request);

        post.Tags.Should().BeEquivalentTo(new[] { originalTag });
        _tagsService.Verify(x => x.GetAllTags(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePost_CallsRepositoryUpdatePost_Once()
    {
        Post post = CreatePost();
        UpdatePostRequest request = new UpdatePostRequest { Title = "New title" };

        await _postsService.UpdatePost(post, request);

        _postsRepository.Verify(x => x.UpdatePost(post), Times.Once);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ReturnsBaseSlug_WhenNoPostsShareSlug()
    {
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        string slug = await _postsService.GenerateUniqueSlugAsync(title);

        slug.Should().Be(baseSlug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ReturnsSecondSuffix_WhenOnlyExactBaseSlugIsTaken()
    {
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(baseSlug)).ReturnsAsync(new List<Post>
        {
            new Post
            {
                Title = title,
                Slug = baseSlug,
                AuthorId = Guid.NewGuid()
            }
        });

        string slug = await _postsService.GenerateUniqueSlugAsync(title);

        slug.Should().Be($"{baseSlug}-2");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_PicksMaxSuffixPlusOne_WhenThereIsAGapInTakenSuffixes()
    {
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(baseSlug)).ReturnsAsync(new List<Post>
        {
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-1",
                AuthorId = Guid.NewGuid()
            },
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-5",
                AuthorId = Guid.NewGuid()
            }
        });

        string slug = await _postsService.GenerateUniqueSlugAsync(title);

        slug.Should().Be($"{baseSlug}-6");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_IgnoresNonNumericSuffixes_WhenComputingNextSuffix()
    {
        const string title = "A Brand New Title";
        string baseSlug = SlugGenerator.Generate(title);

        _postsRepository.Setup(x => x.GetPostsStartingWithSlug(baseSlug)).ReturnsAsync(new List<Post>
        {
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-abc",
                AuthorId = Guid.NewGuid()
            },
            new Post
            {
                Title = title,
                Slug = $"{baseSlug}-1",
                AuthorId = Guid.NewGuid()
            }
        });

        string slug = await _postsService.GenerateUniqueSlugAsync(title);

        slug.Should().Be($"{baseSlug}-2");
    }

    [Fact]
    public async Task CreateCommentForPost_ReturnsComment_WhenCreated()
    {
        Post post = CreatePost();
        const string username = "user";
        const string body = "comment body";
        Comment expectedComment = new Comment
        {
            Username = username,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = post.Id
        };

        _commentsService.Setup(x => x.CreateComment(username, body, post.Id)).ReturnsAsync(expectedComment);

        Comment comment = await _postsService.CreateCommentForPost(post, username, body);

        comment.Should().Be(expectedComment);
        _commentsService.Verify(x => x.CreateComment(username, body, post.Id), Times.Once);
    }
}