using BlogApi.Domain;

namespace BlogApi.Repositories.RefreshTokens;

public interface IRefreshTokensRepository
{
    public Task AddToken(RefreshToken token);
}