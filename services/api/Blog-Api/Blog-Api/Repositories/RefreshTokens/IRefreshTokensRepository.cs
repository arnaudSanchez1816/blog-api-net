using BlogApi.Domain;

namespace BlogApi.Repositories.RefreshTokens;

public interface IRefreshTokensRepository
{
    public Task AddToken(RefreshToken token);

    public Task<RefreshToken?> GetToken(string token, bool asNoTracking = false);

    public Task UpdateToken(RefreshToken token);

    public Task RotateToken(RefreshToken usedToken, RefreshToken newToken);

    public Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default);
}