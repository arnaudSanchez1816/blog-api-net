using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BlogApi.Authorization;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Integration.Extensions;
using BlogApi.Repositories.Comments;
using BlogApi.Repositories.Posts;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class CommentsControllerTests : IntegrationTestBase
{
    private ICommentsRepository _commentsRepository = null!;
    private DataContext _context = null!;
    private Post _post = null!;
    private IPostsRepository _postsRepository = null!;

    public CommentsControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _context = GetRequiredService<DataContext>();
        _postsRepository = GetRequiredService<IPostsRepository>();
        _commentsRepository = GetRequiredService<ICommentsRepository>();

        BlogUser author = new BlogUser
        {
            DisplayName = "Author name",
            UserName = "author@email.com",
            Email = "author@email.com"
        };
        _context.Users.Add(author);
        await _context.SaveChangesAsync();

        _post = new Post
        {
            Title = "Post title",
            Body = "Post body",
            Description = "Post description",
            PublishedAt = DateTimeOffset.UtcNow,
            ReadingTime = 1,
            AuthorId = author.Id,
            Slug = "post-slug"
        };
        await _postsRepository.AddPost(_post);
    }

    #region GetById

    [Fact]
    public async Task GetById_ReturnsComment_WhenCommentExists()
    {
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/comments/{comment.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? commentResponse =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        commentResponse.Should().NotBeNull();
        commentResponse.Id.Should().Be(comment.Id);
        commentResponse.Body.Should().Be(comment.Body);
        commentResponse.Username.Should().Be(comment.Username);
        commentResponse.CreatedAt.Should().BeCloseTo(comment.CreatedAt, TimeSpan.FromMilliseconds(1));
        commentResponse.PostId.Should().Be(comment.PostId);
    }

    [Fact]
    public async Task GetById_Returns400_WhenGivenInvalidId()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/comments/abcd", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_Returns404_WhenCommentDoesNotExists()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/comments/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns403_WhenNoReadPermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync($"api/v1.0/comments/{comment.Id}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UpdateById

    [Fact]
    public async Task UpdateById_ReturnsUpdatedComment_WhenRequestIsValid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        const string newCommentBody = "New comment body";
        const string newCommentUsername = "New comment username";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = newCommentBody,
            Username = newCommentUsername
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? commentResponse =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        commentResponse.Should().NotBeNull();
        commentResponse.Id.Should().Be(comment.Id);
        commentResponse.Body.Should().Be(newCommentBody);
        commentResponse.Username.Should().Be(newCommentUsername);
        commentResponse.CreatedAt.Should().BeCloseTo(comment.CreatedAt, TimeSpan.FromMilliseconds(1));
        commentResponse.PostId.Should().Be(comment.PostId);
    }

    [Fact]
    public async Task UpdateById_LeaveUsernameUnchanged_WhenOnlyUpdatingBody()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        const string newCommentBody = "New comment body";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = newCommentBody
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? commentResponse =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        commentResponse.Should().NotBeNull();
        commentResponse.Body.Should().Be(newCommentBody);
        commentResponse.Username.Should().Be(comment.Username);
    }

    [Fact]
    public async Task UpdateById_LeaveBodyUnchanged_WhenOnlyUpdatingUsername()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        const string newCommentUsername = "New comment username";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Username = newCommentUsername
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? commentResponse =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        commentResponse.Should().NotBeNull();
        commentResponse.Body.Should().Be(comment.Body);
        commentResponse.Username.Should().Be(newCommentUsername);
    }

    [Fact]
    public async Task UpdateById_Returns404_WhenCommentDoesNotExists()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        const string newCommentBody = "New comment body";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = newCommentBody
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{Guid.NewGuid()}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateById_Returns400_WhenIdIsInvalid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        const string newCommentBody = "New comment body";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = newCommentBody
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync("api/v1.0/comments/abcd", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateById_Returns400_WhenUsernameIsEmpty()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        const string newCommentUsername = "";
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Username = newCommentUsername
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateById_Returns400_WhenUsernameIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Username = new string('a', Comment.UsernameMaxLength + 1)
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateById_Returns400_WhenBodyIsEmpty()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = ""
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateById_Returns400_WhenBodyIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Update]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = new string('a', Comment.BodyMaxLength + 1)
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateById_Returns401_WhenNoBearerTokenIsProvided()
    {
        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = "New comment body"
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{Guid.NewGuid()}", request,
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateById_Returns403_WhenUserHasNoUpdatePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        UpdateCommentRequest request = new UpdateCommentRequest
        {
            Body = "New comment body"
        };
        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/comments/{comment.Id}", request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DeleteById

    [Fact]
    public async Task DeleteById_ReturnsDeletedComment_WhenCommentExists()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Delete]);
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/comments/{comment.Id}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? commentResponse =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        commentResponse.Should().NotBeNull();
        commentResponse.Id.Should().Be(comment.Id);
        commentResponse.Body.Should().Be(comment.Body);
        commentResponse.Username.Should().Be(comment.Username);
        commentResponse.CreatedAt.Should().BeCloseTo(comment.CreatedAt, TimeSpan.FromMilliseconds(1));
        commentResponse.PostId.Should().Be(comment.PostId);
    }

    [Fact]
    public async Task DeleteById_Returns404_WhenCommentDoesNotExists()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Delete]);
        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/comments/{Guid.NewGuid()}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteById_Returns400_WhenIdIsInvalid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Comments.Delete]);
        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync("api/v1.0/comments/abcd",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteById_Returns401_WhenNoBearerTokenIsProvided()
    {
        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/comments/{Guid.NewGuid()}",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteById_Returns403_WhenUserHasNoDeletePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();
        Comment comment = new Comment
        {
            Username = "Comment author",
            Body = "Comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = _post.Id
        };
        await _commentsRepository.AddComment(comment);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/comments/{comment.Id}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
