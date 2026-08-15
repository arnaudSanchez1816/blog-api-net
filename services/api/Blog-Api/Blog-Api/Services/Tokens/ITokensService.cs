using System.Security.Claims;
using BlogApi.Domain;

namespace BlogApi.Services.Tokens;

public interface ITokensService
{
    public RefreshToken CreateRefreshToken(BlogUser user);
    public string GenerateAccessToken(BlogUser user, IReadOnlyCollection<Claim>? additionalClaims = null);

    public Task<RefreshToken> GenerateAndSaveRefreshToken(BlogUser user, CancellationToken ct = default);

    public Task<RefreshToken?> GetRefreshToken(string token, bool forceFetchFromDatabase = false,
        CancellationToken ct = default);

    public Task UseRefreshToken(RefreshToken token, RefreshToken replacedByToken, CancellationToken ct = default);
    public Task RevokeRefreshToken(RefreshToken token, CancellationToken ct = default);
}