using BlogApi.Domain;

namespace BlogApi.Repositories.RefreshTokens;

public interface IRefreshTokensRepository
{
    public Task AddToken(RefreshToken token);

    public Task<RefreshToken?> GetToken(string token);

    public Task UpdateToken(RefreshToken token);

    public Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default);
}