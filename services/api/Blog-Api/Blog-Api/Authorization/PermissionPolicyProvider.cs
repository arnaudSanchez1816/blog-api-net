using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BlogApi.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string PermissionPolicyPrefix = "Permissions:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            AuthorizationPolicyBuilder policyBuilder =
                new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
            string permission = policyName.Substring(PermissionPolicyPrefix.Length);
            policyBuilder.AddRequirements(new PermissionRequirement(permission));

            return Task.FromResult(policyBuilder.Build())!;
        }

        return base.GetPolicyAsync(policyName);
    }
}