using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using BlogApi.Domain;
using BlogApi.Options;
using BlogApi.Repositories.RefreshTokens;
using BlogApi.Services.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace BlogApi.Unit.Services;

public class TokensServiceTests : IDisposable
{
    private const string JwtAccessSecret = "this-is-a-super-secret-test-key-32bytes+";

    private static readonly Uri JwtIssuerUri = new Uri("https://blog-api.test/");
    private static readonly Uri JwtAudienceUri = new Uri("https://blog-api.test/");

    private readonly Mock<IRefreshTokensRepository> _refreshTokensRepository;
    private readonly ITokensService _tokensService;

    public TokensServiceTests()
    {
        _refreshTokensRepository = new Mock<IRefreshTokensRepository>();

        IOptions<AppAuthenticationOptions> authOptions = Microsoft.Extensions.Options.Options.Create(
            new AppAuthenticationOptions
            {
                JwtAccessSecret = JwtAccessSecret,
                JwtIssuerUri = JwtIssuerUri,
                JwtAudienceUri = JwtAudienceUri
            });

        _tokensService = new TokensService(authOptions, _refreshTokensRepository.Object);
    }

    public void Dispose()
    {
        _refreshTokensRepository.Reset();
    }

    private static BlogUser CreateUser()
    {
        return new BlogUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            DisplayName = "User Display Name"
        };
    }

    private static TokenValidationParameters ValidationParametersFor(string secret)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = JwtIssuerUri.ToString(),
            ValidAudience = JwtAudienceUri.ToString(),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuerSigningKey = true
        };
    }

    [Fact]
    public void GenerateAccessToken_SetsExpectedClaims_WhenUserIsValid()
    {
        BlogUser user = CreateUser();

        string token = _tokensService.GenerateAccessToken(user);

        JsonWebTokenHandler handler = new JsonWebTokenHandler();
        JsonWebToken jwt = handler.ReadJsonWebToken(token);

        jwt.GetClaim(JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id.ToString());
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        jwt.GetClaim(JwtRegisteredClaimNames.Name).Value.Should().Be(user.UserName);
        jwt.GetClaim(JwtRegisteredClaimNames.Nickname).Value.Should().Be(user.DisplayName);
        jwt.GetClaim(JwtRegisteredClaimNames.Jti).Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_SetsIssuerAndAudience_FromOptions()
    {
        BlogUser user = CreateUser();

        string token = _tokensService.GenerateAccessToken(user);

        JsonWebTokenHandler handler = new JsonWebTokenHandler();
        JsonWebToken jwt = handler.ReadJsonWebToken(token);

        jwt.Issuer.Should().Be(JwtIssuerUri.ToString());
        jwt.Audiences.Should().ContainSingle().Which.Should().Be(JwtAudienceUri.ToString());
    }

    [Fact]
    public void GenerateAccessToken_SetsExpiry_ToSixtyMinutesFromNow()
    {
        BlogUser user = CreateUser();
        DateTime beforeGeneration = DateTime.UtcNow;

        string token = _tokensService.GenerateAccessToken(user);

        JsonWebTokenHandler handler = new JsonWebTokenHandler();
        JsonWebToken jwt = handler.ReadJsonWebToken(token);

        jwt.ValidTo.Should().BeCloseTo(beforeGeneration.AddMinutes(60), 5.Seconds());
    }

    [Fact]
    public async Task GenerateAccessToken_ProducesSignedToken_WhenValidatedWithMatchingSecret()
    {
        BlogUser user = CreateUser();
        string token = _tokensService.GenerateAccessToken(user);
        JsonWebTokenHandler handler = new JsonWebTokenHandler();

        TokenValidationResult result =
            await handler.ValidateTokenAsync(token, ValidationParametersFor(JwtAccessSecret));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAccessToken_FailsValidation_WhenValidatedWithDifferentSecret()
    {
        BlogUser user = CreateUser();
        string token = _tokensService.GenerateAccessToken(user);
        JsonWebTokenHandler handler = new JsonWebTokenHandler();

        TokenValidationResult result =
            await handler.ValidateTokenAsync(token, ValidationParametersFor("a-completely-different-secret-32bytes+"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_GeneratesUniqueJti_WhenCalledMultipleTimesForSameUser()
    {
        BlogUser user = CreateUser();
        JsonWebTokenHandler handler = new JsonWebTokenHandler();

        string firstToken = _tokensService.GenerateAccessToken(user);
        string secondToken = _tokensService.GenerateAccessToken(user);

        string firstJti = handler.ReadJsonWebToken(firstToken).GetClaim(JwtRegisteredClaimNames.Jti).Value;
        string secondJti = handler.ReadJsonWebToken(secondToken).GetClaim(JwtRegisteredClaimNames.Jti).Value;

        firstJti.Should().NotBe(secondJti);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCustomClaims_WhenAdditionalClaimsAreProvided()
    {
        BlogUser user = CreateUser();
        JsonWebTokenHandler handler = new JsonWebTokenHandler();

        const string phoneNumber = "0123456789";
        const string country = "France";
        List<Claim> customClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Country, country),
            new Claim(ClaimTypes.HomePhone, phoneNumber)
        };
        string firstToken = _tokensService.GenerateAccessToken(user, customClaims);

        string countryClaim = handler.ReadJsonWebToken(firstToken).GetClaim(ClaimTypes.Country).Value;
        string phoneClaim = handler.ReadJsonWebToken(firstToken).GetClaim(ClaimTypes.HomePhone).Value;

        countryClaim.Should().Be(country);
        phoneClaim.Should().Be(phoneNumber);
    }

    [Fact]
    public void GenerateAccessToken_Throws_WhenUserEmailIsNull()
    {
        BlogUser user = CreateUser();
        user.Email = null;

        Action act = () => _tokensService.GenerateAccessToken(user);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateAccessToken_Throws_WhenUserNameIsNull()
    {
        BlogUser user = CreateUser();
        user.UserName = null;

        Action act = () => _tokensService.GenerateAccessToken(user);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateRefreshToken_ReturnsTokenLinkedToUser_WhenUserIsValid()
    {
        BlogUser user = CreateUser();

        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        refreshToken.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GenerateRefreshToken_ReturnsUnusedAndNotInvalidatedToken_WhenGenerated()
    {
        BlogUser user = CreateUser();

        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        refreshToken.Used.Should().BeFalse();
        refreshToken.Invalidated.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRefreshToken_SetsExpirationDate_ToThirtyDaysFromNow()
    {
        BlogUser user = CreateUser();
        DateTimeOffset beforeGeneration = DateTimeOffset.UtcNow;

        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        refreshToken.ExpirationDate.Should().BeCloseTo(beforeGeneration.AddDays(30), 5.Seconds());
        refreshToken.CreationDate.Should().BeCloseTo(beforeGeneration, 5.Seconds());
    }

    [Fact]
    public async Task GenerateRefreshToken_ReturnsUrlSafeNonEmptyToken_WhenGenerated()
    {
        BlogUser user = CreateUser();

        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        refreshToken.Token.Should().NotBeNullOrWhiteSpace();
        byte[] decoded = Convert.FromBase64String(refreshToken.Token);
        decoded.Should().HaveCount(32);
    }

    [Fact]
    public async Task GenerateRefreshToken_GeneratesUniqueToken_WhenCalledMultipleTimesForSameUser()
    {
        BlogUser user = CreateUser();

        RefreshToken firstToken = await _tokensService.GenerateRefreshToken(user);
        RefreshToken secondToken = await _tokensService.GenerateRefreshToken(user);

        firstToken.Token.Should().NotBe(secondToken.Token);
    }

    [Fact]
    public async Task GenerateRefreshToken_PersistsTokenThroughRepository_WhenGenerated()
    {
        BlogUser user = CreateUser();

        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        _refreshTokensRepository.Verify(x => x.AddToken(refreshToken), Times.Once);
    }

    [Fact]
    public async Task UseRefreshToken_SetToken_AsUsed()
    {
        BlogUser user = CreateUser();
        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        await _tokensService.UseRefreshToken(refreshToken);

        refreshToken.Used.Should().BeTrue();
        _refreshTokensRepository.Verify(x => x.UpdateToken(refreshToken), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshToken_SetToken_AsInvalidated()
    {
        BlogUser user = CreateUser();
        RefreshToken refreshToken = await _tokensService.GenerateRefreshToken(user);

        await _tokensService.RevokeRefreshToken(refreshToken);

        refreshToken.Invalidated.Should().BeTrue();
        _refreshTokensRepository.Verify(x => x.UpdateToken(refreshToken), Times.Once);
    }
}