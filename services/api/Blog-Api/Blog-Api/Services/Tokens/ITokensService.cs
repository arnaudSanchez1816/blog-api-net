using System.Security.Claims;
using BlogApi.Domain;

namespace BlogApi.Services.Tokens;

public interface ITokensService
{
    public string GenerateAccessToken(BlogUser user, IReadOnlyCollection<Claim>? additionalClaims = null);

    public Task<RefreshToken> GenerateRefreshToken(BlogUser user);
}