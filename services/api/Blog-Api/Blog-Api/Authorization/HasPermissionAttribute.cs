using Microsoft.AspNetCore.Authorization;

namespace BlogApi.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public string Permission
    {
        get => Policy?.Substring(PermissionPolicyProvider.PermissionPolicyPrefix.Length) ?? string.Empty;
        set => Policy = $"{PermissionPolicyProvider.PermissionPolicyPrefix}{value}";
    }

    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}