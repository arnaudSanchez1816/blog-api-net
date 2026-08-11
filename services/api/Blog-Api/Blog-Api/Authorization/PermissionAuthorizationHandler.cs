using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        bool hasClaim = context.User.HasClaim(c =>
            c.Type == CustomClaimTypes.Permission && c.Value == requirement.Permission);
        bool isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        bool hasAnonClaim = !isAuthenticated &&
                            Roles.Permissions.AnonymousPermissions.Contains(requirement.Permission);
        if (hasClaim || hasAnonClaim)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}