using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlogApi.Domain;
using BlogApi.Options;
using BlogApi.Repositories.RefreshTokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlogApi.Services.Jwt;

public class TokenService : ITokenService
{
    private readonly IOptions<AppAuthenticationOptions> _authOptions;
    private readonly IRefreshTokensRepository _refreshTokensRepository;

    public TokenService(IOptions<AppAuthenticationOptions> authOptions,
        IRefreshTokensRepository refreshTokensRepository)
    {
        _authOptions = authOptions;
        _refreshTokensRepository = refreshTokensRepository;
    }

    public string GenerateAccessToken(BlogUser user)
    {
        AppAuthenticationOptions authenticationOptions = _authOptions.Value;
        JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
        byte[] key = Encoding.UTF8.GetBytes(authenticationOptions.JwtAccessSecret);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Nickname, user.DisplayName!)
        ];

        SecurityTokenDescriptor jwtDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = authenticationOptions.JwtIssuerUri.ToString(),
            Audience = authenticationOptions.JwtAudienceUri.ToString(),
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
}