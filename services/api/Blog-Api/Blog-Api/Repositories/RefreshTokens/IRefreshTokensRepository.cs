using BlogApi.Domain;

namespace BlogApi.Repositories.RefreshTokens;

public interface IRefreshTokensRepository
{
    public Task AddToken(RefreshToken token, CancellationToken ct = default);

    public Task<RefreshToken?> GetToken(string token, bool asNoTracking = false, CancellationToken ct = default);

    public Task UpdateToken(RefreshToken token, CancellationToken ct = default);

    public Task RotateToken(RefreshToken usedToken, RefreshToken newToken, CancellationToken ct = default);

    public Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default, CancellationToken ct = default);
}