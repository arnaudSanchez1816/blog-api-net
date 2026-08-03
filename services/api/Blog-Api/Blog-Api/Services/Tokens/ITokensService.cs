using BlogApi.Domain;

namespace BlogApi.Services.Tokens;

public interface ITokensService
{
    public string GenerateAccessToken(BlogUser user);

    public Task<RefreshToken> GenerateRefreshToken(BlogUser user);
}