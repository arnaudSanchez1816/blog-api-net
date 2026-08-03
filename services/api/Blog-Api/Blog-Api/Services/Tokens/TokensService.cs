using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlogApi.Domain;
using BlogApi.Options;
using BlogApi.Repositories.RefreshTokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlogApi.Services.Tokens;

public class TokensService : ITokensService
{
    private readonly AppAuthenticationOptions _authOptions;
    private readonly IRefreshTokensRepository _refreshTokensRepository;

    public TokensService(IOptions<AppAuthenticationOptions> authOptions,
        IRefreshTokensRepository refreshTokensRepository)
    {
        _authOptions = authOptions.Value;
        _refreshTokensRepository = refreshTokensRepository;
    }

    public string GenerateAccessToken(BlogUser user, IReadOnlyCollection<Claim>? additionalClaims = null)
    {
        JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
        byte[] key = Encoding.UTF8.GetBytes(_authOptions.JwtAccessSecret);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Nickname, user.DisplayName!)
        ];
        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        SecurityTokenDescriptor jwtDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = _authOptions.JwtIssuerUri.ToString(),
            Audience = _authOptions.JwtAudienceUri.ToString(),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.CreateToken(jwtDescriptor);
    }

    public async Task<RefreshToken> GenerateRefreshToken(BlogUser user)
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        RefreshToken refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
            Used = false,
            Invalidated = false,
            UserId = user.Id
        };

        await _refreshTokensRepository.AddToken(refreshToken);

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshToken(string token)
    {
        return await _refreshTokensRepository.GetToken(token);
    }

    public async Task UseRefreshToken(RefreshToken token)
    {
        token.Used = true;
        await _refreshTokensRepository.UpdateToken(token);
    }

    public async Task RevokeRefreshToken(RefreshToken token)
    {
        token.Invalidated = true;
        await _refreshTokensRepository.UpdateToken(token);
    }
}