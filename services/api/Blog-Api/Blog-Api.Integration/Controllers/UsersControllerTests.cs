using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Integration.Extensions;
using BlogApi.Repositories.Posts;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class UsersControllerTests : IntegrationTestBase
{
    private IPostsRepository _postsRepository = null!;

    public UsersControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override Task OnInitializeAsync()
    {
        _postsRepository = GetRequiredService<IPostsRepository>();

        return Task.CompletedTask;
    }

    private async Task<Post> CreatePost(Guid authorId, string slug, bool published = true)
    {
        Post post = new Post
        {
            Title = $"Post {slug}",
            Slug = slug,
            AuthorId = authorId,
            PublishedAt = published ? DateTimeOffset.UtcNow : null
        };
        await _postsRepository.AddPost(post);
        return post;
    }

    #region GetCurrentUser

    [Fact]
    public async Task GetCurrentUser_Returns200_WhenUserIsAuthenticated()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/users/me",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UserResponse? userDetails =
            await response.Content.ReadFromJsonAsync<UserResponse>(TestContext.Current.CancellationToken);
        userDetails.Should().NotBeNull();
        userDetails.Id.Should().Be(user.Id);
        userDetails.Email.Should().Be(user.Email);
        userDetails.Name.Should().Be(user.DisplayName);
    }

    [Fact]
    public async Task GetCurrentUser_Returns401_WhenNoTokenProvided()
    {
        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/users/me",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_Returns401_WhenTokenIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/users/me",
                "badTokenOk",
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetCurrentUserPosts

    [Fact]
    public async Task GetCurrentUserPosts_ReturnsEmpty_WhenUserHasNoPosts()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts?pageNumber=1&pageSize=10",
            bearerToken,
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
    public async Task GetCurrentUserPosts_ReturnsOnlyCurrentUsersPosts_WhenOtherUsersHavePosts()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();
        (BlogUser otherUser, _) = await RegisterAuthenticatedUser("Other user");
        Post ownPost = await CreatePost(user.Id, "own-post");
        await CreatePost(otherUser.Id, "other-users-post");

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == ownPost.Slug);
    }

    [Fact]
    public async Task GetCurrentUserPosts_ShouldOnlyReturnUserPosts_EvenWhenPassingAnAuthorIdInQuery()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();
        (BlogUser otherUser, _) = await RegisterAuthenticatedUser("Other user");
        Post ownPost = await CreatePost(user.Id, "own-post");
        await CreatePost(otherUser.Id, "other-users-post");

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            $"api/v1.0/users/me/posts?author={otherUser.Id}",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == ownPost.Slug);
    }

    [Fact]
    public async Task GetCurrentUserPosts_IncludesUnpublishedPosts_WhenUnpublishedParamIsOmitted()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();
        Post draftPost = await CreatePost(user.Id, "draft-post", false);

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == draftPost.Slug);
    }

    [Fact]
    public async Task GetCurrentUserPosts_ShouldRespect_PaginationQueryParams()
    {
        (BlogUser user, string bearerToken) = await RegisterAuthenticatedUser();
        for (int i = 0; i < 15; i++)
            await CreatePost(user.Id, $"post-{i}");

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts?pageNumber=2&pageSize=10",
            bearerToken,
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
    public async Task GetCurrentUserPosts_Returns400_WhenSortByIsInvalid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts?sortBy=invalidSortBy",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentUserPosts_Returns401_WhenNoTokenProvided()
    {
        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts",
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUserPosts_Returns401_WhenTokenIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetWithBearerAsync(
            "api/v1.0/users/me/posts",
            "badTokenOk",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}