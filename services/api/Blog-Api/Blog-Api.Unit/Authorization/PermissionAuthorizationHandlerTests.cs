using System.Security.Claims;
using AwesomeAssertions;
using BlogApi.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Unit.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private const string RequiredPermission = "tags.delete";

    private readonly PermissionAuthorizationHandler _handler = new PermissionAuthorizationHandler();

    private static ClaimsPrincipal CreateUser(params string[] permissions)
    {
        List<Claim> claims = permissions.Select(p => new Claim(CustomClaimTypes.Permission, p)).ToList();
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }

    private static AuthorizationHandlerContext CreateContext(PermissionRequirement requirement,
        ClaimsPrincipal user, object? resource = null)
    {
        return new AuthorizationHandlerContext([requirement], user, resource);
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_WhenUserHasMatchingPermissionClaim()
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        ClaimsPrincipal user = CreateUser(RequiredPermission);
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Succeeds_WhenUserHasMultiplePermissionClaimsIncludingRequiredOne()
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        ClaimsPrincipal user = CreateUser("tags.read", "tags.create", RequiredPermission);
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenUserDoesNotHaveMatchingPermissionClaim()
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        ClaimsPrincipal user = CreateUser("tags.read");
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenUserHasNoPermissionClaims()
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity());
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenClaimValueDiffersOnlyByCase()
    {
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        ClaimsPrincipal user = CreateUser(RequiredPermission.ToUpperInvariant());
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_Fails_WhenClaimTypeDoesNotMatchPermissionClaimType()
    {
        ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("some-other-claim-type", RequiredPermission)
        ]));
        PermissionRequirement requirement = new PermissionRequirement(RequiredPermission);
        AuthorizationHandlerContext context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}