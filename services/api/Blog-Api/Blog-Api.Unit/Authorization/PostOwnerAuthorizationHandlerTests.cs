using System.Security.Claims;
using AwesomeAssertions;
using BlogApi.Authorization;
using BlogApi.Domain;
using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Unit.Authorization;

public class PostOwnerAuthorizationHandlerTests
{
    private const string RequiredPermission = "posts.delete";

    private readonly PostOwnerAuthorizationHandler _handler = new PostOwnerAuthorizationHandler();

    private static ClaimsPrincipal CreateAuthenticatedUser(Guid? userId)
    {
        List<Claim> claims = userId is null
            ? []
            : [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }

    private static Post CreatePost(Guid authorId)
    {
        return new Post
        {
            Title = "Post title",
            Slug = "post-title",
            AuthorId = authorId
        };
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, object? resource)
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        return new AuthorizationHandlerContext([requirement], user, resource);
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_WhenUserIsResourceOwner()
    {
        Guid userId = Guid.NewGuid();
        Post post = CreatePost(userId);
        ClaimsPrincipal user = CreateAuthenticatedUser(userId);
        AuthorizationHandlerContext context = CreateContext(user, post);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenUserIsNotResourceOwner()
    {
        Post post = CreatePost(Guid.NewGuid());
        ClaimsPrincipal user = CreateAuthenticatedUser(Guid.NewGuid());
        AuthorizationHandlerContext context = CreateContext(user, post);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_DoesNotSucceed_WhenResourceIsNotAPost()
    {
        ClaimsPrincipal user = CreateAuthenticatedUser(Guid.NewGuid());
        AuthorizationHandlerContext context = CreateContext(user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenUserIsAnonymous()
    {
        Post post = CreatePost(Guid.NewGuid());
        ClaimsPrincipal anonymousUser = new ClaimsPrincipal();
        AuthorizationHandlerContext context = CreateContext(anonymousUser, post);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Throws_WhenAuthenticatedUserHasNoNameIdentifierClaim()
    {
        Post post = CreatePost(Guid.NewGuid());
        ClaimsPrincipal user = CreateAuthenticatedUser(null);
        AuthorizationHandlerContext context = CreateContext(user, post);

        Func<Task> act = async () => await _handler.HandleAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}