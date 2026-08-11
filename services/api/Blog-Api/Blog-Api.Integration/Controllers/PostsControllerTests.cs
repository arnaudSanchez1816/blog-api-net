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
using BlogApi.Repositories.Tags;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class PostsControllerTests : IntegrationTestBase
{
    private const string InvalidSlug = "@--";

    private static readonly string TitleAtMaxLength = new string('a', Post.TitleMaxLength);
    private static readonly string TitleOverMaxLength = new string('a', Post.TitleMaxLength + 1);
    private static readonly string CommentBodyOverMaxLength = new string('a', Comment.BodyMaxLength + 1);
    private static readonly string CommentUsernameOverMaxLength = new string('a', Comment.UsernameMaxLength + 1);

    private BlogUser _author = null!;
    private ICommentsRepository _commentsRepository = null!;
    private DataContext _context = null!;
    private IPostsRepository _postsRepository = null!;
    private ITagsRepository _tagsRepository = null!;

    public PostsControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _postsRepository = GetRequiredService<IPostsRepository>();
        _tagsRepository = GetRequiredService<ITagsRepository>();
        _commentsRepository = GetRequiredService<ICommentsRepository>();
        _context = GetRequiredService<DataContext>();

        _author = await CreateUser("author@email.com", "Author name");
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

    // ---- GetPostCommentsBySlug ----

    [Fact]
    public async Task GetPostCommentsBySlug_ReturnsEmptyList_WhenPostHasNoComments()
    {
        Post post = new Post
        {
            Title = "Post without comments",
            Slug = "post-without-comments",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Comments.Should().BeEmpty();
        body.Metadata.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_ReturnsComments_WhenPostHasComments()
    {
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        Comment comment1 = new Comment
        {
            Username = "commenter-one",
            Body = "First comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = post.Id
        };
        Comment comment2 = new Comment
        {
            Username = "commenter-two",
            Body = "Second comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = post.Id
        };
        await _commentsRepository.AddComment(comment1);
        await _commentsRepository.AddComment(comment2);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Comments.Should().HaveCount(2);
        body.Metadata.Count.Should().Be(2);
        body.Comments.Should()
            .Contain(c =>
                c.Username == comment1.Username && c.Body == comment1.Body && c.PostId == post.Id);
        body.Comments.Should()
            .Contain(c =>
                c.Username == comment2.Username && c.Body == comment2.Body && c.PostId == post.Id);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_Returns404_WhenSlugDoesNotExist()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts/does-not-exist/comments",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1.0/posts/{InvalidSlug}/comments",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- CreatePostComment ----

    [Fact]
    public async Task CreatePostComment_ReturnsCreatedComment_WhenGivenValidRequest()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? body =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().NotBeEmpty();
        body.Username.Should().Be(request.Username);
        body.Body.Should().Be(request.Body);
        body.PostId.Should().Be(post.Id);
    }

    [Fact]
    public async Task CreatePostComment_PersistsComment_WhenGivenValidRequest()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-persisted",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request,
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse = await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments",
            TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? followUpBody = await followUpResponse.Content
            .ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        followUpBody.Should().NotBeNull();
        followUpBody.Comments.Should().Contain(c => c.Username == request.Username && c.Body == request.Body);
    }

    [Fact]
    public async Task CreatePostComment_Returns404_WhenSlugDoesNotExist()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1.0/posts/does-not-exist/comments",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenSlugIsInvalid()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{InvalidSlug}/comments",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenBodyIsMissing()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-missing-body",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            new { Username = "commenter" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenUsernameIsMissing()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-missing-username",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            new { Body = "Nice post!" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenBodyExceedsMaxLength()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-body-too-long",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request =
            new CreatePostCommentRequest { Username = "commenter", Body = CommentBodyOverMaxLength };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenUsernameExceedsMaxLength()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-username-too-long",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request =
            new CreatePostCommentRequest { Username = CommentUsernameOverMaxLength, Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #region DeletePost

    [Fact]
    public async Task DeletePost_ReturnsDeletedPost_WhenSlugExists()
    {
        (BlogUser user, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Delete]);

        Post post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            AuthorId = user.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);

        HttpResponseMessage followUpResponse =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_DeletePost_WhenUserIsOwner()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();

        Post post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            AuthorId = user.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_DeletePost_WhenUserHasDeletePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Delete]);

        Post post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_Returns404_WhenSlugDoesNotExist()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Delete]);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync("api/v1.0/posts/does-not-exist",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_Returns400_WhenSlugIsInvalid()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Delete]);

        HttpResponseMessage response =
            await HttpClient.DeleteWithBearerAsync($"api/v1.0/posts/{InvalidSlug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePost_Returns401_WhenUnauthenticated()
    {
        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync("api/v1.0/posts/does-not-exist",
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePost_Returns403_WhenNotOwnerAndNoDeletePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        Post post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync($"api/v1.0/posts/{post.Slug}",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UpdatePost

    // ---- UpdatePost ----

    [Fact]
    public async Task UpdatePost_ReturnsUpdatedPost_WhenGivenValidData()
    {
        (BlogUser user, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = user.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title", Body = "Updated Body" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be("Updated Title");
        body.Body.Should().Be("Updated Body");
    }

    [Fact]
    public async Task UpdatePost_Returns404_WhenSlugDoesNotExist()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync("api/v1.0/posts/does-not-exist",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenSlugParamIsInvalid()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{InvalidSlug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenRequestBodyIsEmptyObject()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            new UpdatePostRequest(),
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenTitleIsTooLong()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = TitleOverMaxLength };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenTagsContainInvalidSlug()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Tags = ["java", "not@ValidSlug!"] };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_ReplacesTags_WhenTagsAreGiven()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Tag tag1 = new Tag { Name = "Java", Slug = "java" };
        Tag tag2 = new Tag { Name = "Spring", Slug = "spring" };
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);
        await _tagsRepository.AddTag(tag3);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        post.Tags.Add(tag1);
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest { Tags = ["spring", "docker"] };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Tags.Should().HaveCount(2);
        body.Tags.Should().Contain(t => t.Slug == "spring");
        body.Tags.Should().Contain(t => t.Slug == "docker");
        body.Tags.Should().NotContain(t => t.Slug == "java");
    }

    [Fact]
    public async Task UpdatePost_LeavesBodyUnchanged_WhenOnlyTitleIsGiven()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Updated Title Only");
        body.Body.Should().Be("Original Body");
    }

    [Fact]
    public async Task UpdatePost_LeavesTitleUnchanged_WhenOnlyBodyIsGiven()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Body = "Updated Body Only" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Original Title");
        body.Body.Should().Be("Updated Body Only");
    }

    [Fact]
    public async Task UpdatePost_LeavesTagsUnchanged_WhenTagsAreOmitted()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Tag tag = new Tag { Name = "Java", Slug = "java" };
        await _tagsRepository.AddTag(tag);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        post.Tags.Add(tag);
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Updated Title Only");
        body.Tags.Should().HaveCount(1);
        body.Tags.Should().Contain(t => t.Slug == "java");
    }

    [Fact]
    public async Task UpdatePost_PublishesPost_WhenIsPublishedIsTrue()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { IsPublished = true };
        DateTimeOffset before = DateTimeOffset.UtcNow;

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().NotBeNull();
        body.PublishedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdatePost_HidesPost_WhenIsPublishedIsFalse()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { IsPublished = false };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePost_LeavesPublishedAtUnchanged_WhenIsPublishedIsNotProvided()
    {
        (BlogUser author, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);
        DateTimeOffset originalPublishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = author.Id,
            PublishedAt = originalPublishedAt
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().BeCloseTo(originalPublishedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdatePost_Returns401_WhenUnauthenticated()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest
        {
            Body = "New body"
        };
        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePost_Returns403_WhenIsNotOwnerAndNoUpdatePermissions()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([]);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest
        {
            Body = "New body"
        };
        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePost_ReturnsUpdatedPost_WhenHasUpdatePermission()
    {
        (_, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Update]);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest
        {
            Body = "New body from moderator"
        };
        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? postResponse =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        postResponse.Should().NotBeNull();
        postResponse.Body.Should().Be(request.Body);
    }

    [Fact]
    public async Task UpdatePost_ReturnsUpdatedPost_WhenIsOwner()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = user.Id
        };
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest
        {
            Body = "New body from owner"
        };
        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? postResponse =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        postResponse.Should().NotBeNull();
        postResponse.Body.Should().Be(request.Body);
    }

    #endregion

    #region CreatePost

    [Fact]
    public async Task CreatePost_ReturnsCreatedPost_WhenGivenValidTitle()
    {
        (BlogUser user, string bearerToken) =
            await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);
        CreatePostRequest request = new CreatePostRequest { Title = "Post title" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be(request.Title);
        body.Author.Id.Should().Be(user.Id);
        response.Headers.Location.Should().NotBeNull();

        HttpResponseMessage locationResponse =
            await HttpClient.GetWithBearerAsync(response.Headers.Location,
                bearerToken,
                TestContext.Current.CancellationToken);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? locationBody =
            await locationResponse.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        locationBody.Should().NotBeNull();
        locationBody.Title.Should().Be(request.Title);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsMissing()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);
        HttpResponseMessage response = await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
            new { },
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);
        CreatePostRequest request = new CreatePostRequest { Title = TitleOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_ReturnsCreatedPost_WhenTitleIsExactlyAtMaxLength()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);
        CreatePostRequest request = new CreatePostRequest { Title = TitleAtMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsBlank()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);
        CreatePostRequest request = new CreatePostRequest { Title = "        " };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsNull()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Posts.Create]);

        HttpResponseMessage response = await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
            new { Title = (string?)null },
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Returns401_WhenNoBearerTokenIsProvided()
    {
        CreatePostRequest request = new CreatePostRequest { Title = "Unauthentitcated test post title" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePost_Returns403_WhenUserHasNoCreatePermission()
    {
        await CreateRoleWithPermissions("Reader", [Permissions.Posts.Read]);
        (_, string bearerToken) = await RegisterAuthenticatedUser(roles: ["Reader"]);

        CreatePostRequest request = new CreatePostRequest { Title = "Forbidden test post title" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/posts",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetBySlug

    // ---- GetBySlug ----

    [Fact]
    public async Task GetBySlug_ReturnsPost_WhenPostExists()
    {
        Post post = new Post
        {
            Title = "Test Post Title",
            Slug = "test-post-title",
            Description = "Test post description",
            Body = "Test post body content",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);
        body.Slug.Should().Be(post.Slug);
        body.Description.Should().Be(post.Description);
        body.Body.Should().Be(post.Body);
        body.PublishedAt.Should().BeCloseTo(post.PublishedAt.Value, TimeSpan.FromMilliseconds(20));
        body.Author.Should().NotBeNull();
        body.Author.Id.Should().Be(_author.Id);
        body.Author.Name.Should().Be(_author.DisplayName);
        body.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySlug_ReturnsPost_WithTags_WhenPostHasTags()
    {
        Tag tag1 = new Tag { Name = "Java", Slug = "java" };
        Tag tag2 = new Tag { Name = "Spring", Slug = "spring" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        Post post = new Post
        {
            Title = "Post with Tags",
            Slug = "post-with-tags",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        post.Tags.Add(tag1);
        post.Tags.Add(tag2);
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Tags.Should().HaveCount(2);
        body.Tags.Should().Contain(t => t.Slug == "java");
        body.Tags.Should().Contain(t => t.Slug == "spring");
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenSlugDoesNotExist()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenPostIsUnpublished()
    {
        Post post = new Post
        {
            Title = "Draft Post",
            Slug = "draft-post",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenUserIsNotOwnerAndNoPermissions()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();
        Post post = new Post
        {
            Title = "Draft Post",
            Slug = "draft-post",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{InvalidSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBySlug_ReturnsDraftPost_WhenUserIsOwner()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();

        Post post = new Post
        {
            Title = "Test Post Title",
            Slug = "test-post-title",
            Description = "Test post description",
            Body = "Test post body content",
            AuthorId = user.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);
        body.Slug.Should().Be(post.Slug);
        body.Description.Should().Be(post.Description);
        body.Body.Should().Be(post.Body);
        body.PublishedAt.Should().BeNull();
        body.Author.Should().NotBeNull();
        body.Author.Id.Should().Be(user.Id);
        body.Author.Name.Should().Be(user.DisplayName);
        body.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySlug_ReturnsDraftPost_WhenUserHasPermissions()
    {
        await CreateRoleWithPermissions("Admin", [Permissions.Posts.ReadUnpublished]);
        (_, string bearerToken) = await RegisterAuthenticatedUser(roles: ["Admin"]);

        Post post = new Post
        {
            Title = "Test Post Title",
            Slug = "test-post-title",
            Description = "Test post description",
            Body = "Test post body content",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync($"api/v1.0/posts/{post.Slug}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);
        body.Slug.Should().Be(post.Slug);
        body.Description.Should().Be(post.Description);
        body.Body.Should().Be(post.Body);
        body.PublishedAt.Should().BeNull();
        body.Author.Should().NotBeNull();
        body.Author.Id.Should().Be(_author.Id);
        body.Author.Name.Should().Be(_author.DisplayName);
        body.Tags.Should().BeEmpty();
    }

    #endregion

    #region GetPosts

    [Fact]
    public async Task GetPosts_ReturnsEmpty_WhenNoPostsExist()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=1&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
        body.Metadata.Count.Should().Be(0);
        body.Metadata.PageNumber.Should().Be(1);
        body.Metadata.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPosts_ReturnsPosts_WhenPublishedPostsExist()
    {
        Post post = new Post
        {
            Title = "Test Post",
            Slug = "test-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageNumber=1&pageSize=10", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == post.Slug && p.Title == post.Title);
        body.Metadata.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_MetadataSortByIsNull_WhenSortByIsOmitted()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.SortBy.Should().BeNull();
    }

    [Fact]
    public async Task GetPosts_MetadataSortBy_IsSortByGivenInQuery()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?sortBy=id", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.SortBy.Should().Be("id");
    }

    [Fact]
    public async Task GetPosts_Returns400_WhenSortByIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?sortBy=invalidSortBy",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_Returns400_WhenTagSlugIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1.0/posts?tags={InvalidSlug}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_BindsTagsQueryParam_WhenFilteringByTags()
    {
        Tag javaTag = new Tag { Name = "Java", Slug = "java" };
        Tag dockerTag = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(javaTag);
        await _tagsRepository.AddTag(dockerTag);

        Post postWithJavaTag = new Post
        {
            Title = "Java post",
            Slug = "java-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        postWithJavaTag.Tags.Add(javaTag);
        Post postWithDockerTag = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        postWithDockerTag.Tags.Add(dockerTag);
        await _postsRepository.AddPost(postWithJavaTag);
        await _postsRepository.AddPost(postWithDockerTag);

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?tags=java", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "java-post");
    }

    [Fact]
    public async Task GetPosts_BindsAuthorQueryParam_WhenFilteringByAuthor()
    {
        BlogUser author1 = _author;
        BlogUser author2 = await CreateUser("author2@email.com", "Author2 name");
        Post postWithAuthor1 = new Post
        {
            Title = "Java post",
            Slug = "java-post",
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
        await _postsRepository.AddPost(postWithAuthor1);
        await _postsRepository.AddPost(postWithAuthor2);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts?author={author2.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "docker-post");
    }

    [Fact]
    public async Task GetPosts_RespectsPaginationQueryParams()
    {
        for (int i = 0; i < 15; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });

        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=2&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(5);
        body.Metadata.Count.Should().Be(15);
        body.Metadata.PageNumber.Should().Be(2);
        body.Metadata.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPosts_ClampsPageNumberToOne_WhenPageNumberIsNegative()
    {
        for (int i = 0; i < 5; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });

        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=-50&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(5);
        body.Metadata.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_ClampsPageSizeToDefault_WhenPageSizeIsTooSmall()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageSize=-30", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_ClampsPageSizeToMax_WhenPageSizeIsTooBig()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageSize=999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetPosts_ExcludesUnpublishedPosts_WhenUnpublishedParamIsOmitted()
    {
        string role = "Admin";
        await CreateRoleWithPermissions(role, [Permissions.Posts.ReadUnpublished]);
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser(roles: [role]);
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = user.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/posts", bearerToken, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_ExcludesUnpublishedPosts_WhenUnpublishedParamIsFalse()
    {
        string role = "Admin";
        await CreateRoleWithPermissions(role, [Permissions.Posts.ReadUnpublished]);
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser(roles: [role]);
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = user.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/posts?unpublished=false",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_IncludesUnpublishedPosts_WhenUnpublishedParamIsTrue()
    {
        string role = "Admin";
        await CreateRoleWithPermissions(role, [Permissions.Posts.ReadUnpublished]);
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser(roles: [role]);
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = user.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/posts?unpublished=true",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "draft-post");
    }

    [Fact]
    public async Task GetPosts_ExcludesUnpublishedPosts_WhenUserHasMissingPermission()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = user.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/posts?unpublished=true",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
    }

    #endregion
}