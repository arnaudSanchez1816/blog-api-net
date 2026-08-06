using BlogApi.Domain;
using BlogApi.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Authorization;

public class PostOwnerAuthorizationHandler : AuthorizationHandler<PermissionRequirement, Post>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement, Post resource)
    {
        if (context.User.GetUserId() == resource.AuthorId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}