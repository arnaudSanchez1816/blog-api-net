using AwesomeAssertions;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.Comments;
using BlogApi.Repositories.Posts;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class CommentsRepositoryTests : IntegrationTestBase
{
    private BlogUser _author = null!;
    private ICommentsRepository _commentsRepository = null!;
    private DataContext _context = null!;
    private Post _post = null!;
    private IPostsRepository _postsRepository = null!;

    public CommentsRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _commentsRepository = GetRequiredService<ICommentsRepository>();
        _postsRepository = GetRequiredService<IPostsRepository>();
        _context = GetRequiredService<DataContext>();

        _author = new BlogUser
        {
            UserName = "author@example.com",
            Email = "author@example.com",
            DisplayName = "Author Name"
        };
        _context.Users.Add(_author);

        _post = new Post
        {
            Slug = "post-with-comments",
            Title = "Post",
            Description = "Post description",
            Body = "Post body",
            ReadingTime = 2,
            PublishedAt = DateTimeOffset.UtcNow,
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(_post);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCommentById_ReturnsComment_WhenCommentExists()
    {
        Comment comment = new Comment
        {
            Body = "Comment body",
            Username = "Username",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        Comment? foundComment = await _commentsRepository.GetCommentById(comment.Id);

        foundComment.Should().NotBeNull();
        foundComment.Should().BeEquivalentTo(comment);
    }

    [Fact]
    public async Task GetCommentById_ReturnsNull_WhenCommentDoesNotExists()
    {
        Comment? foundComment = await _commentsRepository.GetCommentById(Guid.NewGuid());

        foundComment.Should().BeNull();
    }

    [Fact]
    public async Task GetAllCommentsWithPostId_ReturnAllComments_WhenPostExists()
    {
        Comment comment = new Comment
        {
            Body = "Comment body",
            Username = "Username",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        Comment comment2 = new Comment
        {
            Body = "Comment body 2",
            Username = "Username 2",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(10),
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);
        await _commentsRepository.AddComment(comment2);

        List<Comment> comments = await _commentsRepository.GetAllCommentsWithPostId(_post.Id);

        comments.Should().HaveCount(2);
        comments.Should().ContainEquivalentOf(comment);
        comments.Should().ContainEquivalentOf(comment2);
    }

    [Fact]
    public async Task GetAllCommentsWithPostId_ReturnEmpty_WhenPostHasNoComments()
    {
        List<Comment> comments = await _commentsRepository.GetAllCommentsWithPostId(_post.Id);

        comments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCommentsWithPostId_ReturnEmpty_WhenPostDoestNotExists()
    {
        List<Comment> comments = await _commentsRepository.GetAllCommentsWithPostId(Guid.NewGuid());

        comments.Should().BeEmpty();
    }

    [Fact]
    public async Task AddComment_Success_WhenCommentDoNotExists()
    {
        Comment comment = new Comment
        {
            Body = "Comment body",
            Username = "Username",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };

        await _commentsRepository.AddComment(comment);

        Comment? commentAdded = await _commentsRepository.GetCommentById(comment.Id);
        commentAdded.Should().NotBeNull();
        commentAdded.Should().BeEquivalentTo(comment);
    }

    [Fact]
    public async Task DeleteComment_Success_WhenCommentExists()
    {
        Comment comment = new Comment
        {
            Body = "Comment body",
            Username = "Username",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        await _commentsRepository.DeleteComment(comment);

        Comment? deletedComment = await _commentsRepository.GetCommentById(comment.Id);
        deletedComment.Should().BeNull();
    }

    [Fact]
    public async Task DeleteComment_Fail_WhenCommentDoesNotExists()
    {
        Comment comment = new Comment
        {
            Id = Guid.NewGuid(),
            Body = "Comment body",
            Username = "Username",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };

        Func<Task> act = async () => await _commentsRepository.DeleteComment(comment);
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}