using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Repositories.Comments;
using BlogApi.Services.Comments;
using Moq;

namespace BlogApi.Unit.Services;

public class CommentsServiceTests : IDisposable
{
    private readonly Mock<ICommentsRepository> _commentsRepository;
    private readonly ICommentsService _commentsService;

    public CommentsServiceTests()
    {
        _commentsRepository = new Mock<ICommentsRepository>();
        _commentsService = new CommentsService(_commentsRepository.Object);
    }

    public void Dispose()
    {
        _commentsRepository.Reset();
    }

    [Fact]
    public async Task GetCommentById_ReturnsComment_WhenFound()
    {
        Guid id = Guid.NewGuid();
        Comment comment = new Comment
        {
            Username = "user",
            Body = "body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        _commentsRepository.Setup(x => x.GetCommentById(id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);

        Comment? result = await _commentsService.GetCommentById(id, ct: TestContext.Current.CancellationToken);

        result.Should().Be(comment);
        _commentsRepository.Verify(x => x.GetCommentById(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCommentById_ReturnsNull_WhenNotFound()
    {
        Guid id = Guid.NewGuid();

        _commentsRepository.Setup(x => x.GetCommentById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Comment?)null);

        Comment? result = await _commentsService.GetCommentById(id, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull();
        _commentsRepository.Verify(x => x.GetCommentById(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCommentsWithPostId_ReturnsComments_WhenPresent()
    {
        Guid postId = Guid.NewGuid();
        List<Comment> comments = new List<Comment>
        {
            new Comment
            {
                Username = "user1",
                Body = "body1",
                CreatedAt = DateTimeOffset.UtcNow,
                PostId = postId
            },
            new Comment
            {
                Username = "user2",
                Body = "body2",
                CreatedAt = DateTimeOffset.UtcNow,
                PostId = postId
            }
        };

        _commentsRepository.Setup(x => x.GetAllCommentsWithPostId(postId, It.IsAny<CancellationToken>())).ReturnsAsync(comments);

        List<Comment> result = await _commentsService.GetAllCommentsWithPostId(postId, ct: TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(comments);
        _commentsRepository.Verify(x => x.GetAllCommentsWithPostId(postId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCommentsWithPostId_ReturnsEmptyList_WhenNoneFound()
    {
        Guid postId = Guid.NewGuid();

        _commentsRepository.Setup(x => x.GetAllCommentsWithPostId(postId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Comment>());

        List<Comment> result = await _commentsService.GetAllCommentsWithPostId(postId, ct: TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        _commentsRepository.Verify(x => x.GetAllCommentsWithPostId(postId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateComment_ReturnsComment_WhenValid()
    {
        const string username = "user";
        const string body = "body text";
        Guid postId = Guid.NewGuid();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        Comment createdComment = await _commentsService.CreateComment(username, body, postId, ct: TestContext.Current.CancellationToken);

        DateTimeOffset after = DateTimeOffset.UtcNow;

        createdComment.Username.Should().Be(username);
        createdComment.Body.Should().Be(body);
        createdComment.PostId.Should().Be(postId);
        createdComment.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        _commentsRepository.Verify(x => x.AddComment(createdComment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_DeletesComment()
    {
        Comment comment = new Comment
        {
            Username = "user",
            Body = "body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        await _commentsService.DeleteComment(comment, ct: TestContext.Current.CancellationToken);

        _commentsRepository.Verify(x => x.DeleteComment(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComment_UpdatesUsername_WhenUsernameProvided()
    {
        const string originalBody = "original body";
        Comment comment = new Comment
        {
            Username = "original-user",
            Body = originalBody,
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        await _commentsService.UpdateComment(comment, "new-user", null, ct: TestContext.Current.CancellationToken);

        comment.Username.Should().Be("new-user");
        comment.Body.Should().Be(originalBody);
        _commentsRepository.Verify(x => x.UpdateComment(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComment_UpdatesBody_WhenBodyProvided()
    {
        const string originalUsername = "original-user";
        Comment comment = new Comment
        {
            Username = originalUsername,
            Body = "original body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        await _commentsService.UpdateComment(comment, null, "new body", ct: TestContext.Current.CancellationToken);

        comment.Username.Should().Be(originalUsername);
        comment.Body.Should().Be("new body");
        _commentsRepository.Verify(x => x.UpdateComment(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComment_UpdatesUsernameAndBody_WhenBothProvided()
    {
        Comment comment = new Comment
        {
            Username = "original-user",
            Body = "original body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        await _commentsService.UpdateComment(comment, "new-user", "new body", ct: TestContext.Current.CancellationToken);

        comment.Username.Should().Be("new-user");
        comment.Body.Should().Be("new body");
        _commentsRepository.Verify(x => x.UpdateComment(comment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComment_SkipsRepositoryCall_WhenNeitherProvided()
    {
        const string originalUsername = "original-user";
        const string originalBody = "original body";
        Comment comment = new Comment
        {
            Username = originalUsername,
            Body = originalBody,
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid()
        };

        await _commentsService.UpdateComment(comment, null, null, ct: TestContext.Current.CancellationToken);

        comment.Username.Should().Be(originalUsername);
        comment.Body.Should().Be(originalBody);
        _commentsRepository.Verify(x => x.UpdateComment(comment, It.IsAny<CancellationToken>()), Times.Never);
    }
}
