using BlogApi.Domain;

namespace BlogApi.Services.Jwt;

public interface ITokenService
{
    public string GenerateAccessToken(BlogUser user);

    public Task<RefreshToken> GenerateRefreshToken(BlogUser user);
}