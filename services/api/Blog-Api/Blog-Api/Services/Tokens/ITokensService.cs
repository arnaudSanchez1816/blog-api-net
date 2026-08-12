using System.Security.Claims;
using BlogApi.Domain;

namespace BlogApi.Services.Tokens;

public interface ITokensService
{
    public RefreshToken CreateRefreshToken(BlogUser user);
    public string GenerateAccessToken(BlogUser user, IReadOnlyCollection<Claim>? additionalClaims = null);

    public Task<RefreshToken> GenerateAndSaveRefreshToken(BlogUser user);

    public Task<RefreshToken?> GetRefreshToken(string token);
    public Task UseRefreshToken(RefreshToken token, RefreshToken replacedByToken);
    public Task RevokeRefreshToken(RefreshToken token);
}