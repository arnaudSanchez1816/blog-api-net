using AwesomeAssertions;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.RefreshTokens;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class RefreshTokensRepositoryTests : IntegrationTestBase
{
    private BlogUser _user = null!;
    private DataContext _context = null!;
    private IRefreshTokensRepository _refreshTokensRepository = null!;

    public RefreshTokensRepositoryTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _refreshTokensRepository = GetRequiredService<IRefreshTokensRepository>();
        _context = GetRequiredService<DataContext>();

        _user = new BlogUser
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            DisplayName = "User Name"
        };
        _context.Users.Add(_user);
        await _context.SaveChangesAsync();
    }

    private RefreshToken MakeToken(string token, DateTimeOffset expirationDate, bool used = false,
        bool invalidated = false)
    {
        return new RefreshToken
        {
            Token = token,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate,
            Used = used,
            Invalidated = invalidated,
            UserId = _user.Id
        };
    }

    [Fact]
    public async Task DeleteExpiredTokens_RemovesExpiredToken_WhenNoBufferIsGiven()
    {
        // Arrange
        RefreshToken expiredToken = MakeToken("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        await _refreshTokensRepository.AddToken(expiredToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens();

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("expired-token");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_KeepsActiveToken_WhenNoBufferIsGiven()
    {
        // Arrange
        RefreshToken activeToken = MakeToken("active-token", DateTimeOffset.UtcNow.AddMinutes(30));
        await _refreshTokensRepository.AddToken(activeToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens();

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("active-token");
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_KeepsRecentlyExpiredToken_WhenWithinBuffer()
    {
        // Arrange
        RefreshToken recentlyExpiredToken =
            MakeToken("recently-expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        await _refreshTokensRepository.AddToken(recentlyExpiredToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(TimeSpan.FromHours(1));

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("recently-expired-token");
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_RemovesToken_WhenExpiredForLongerThanBuffer()
    {
        // Arrange
        RefreshToken longExpiredToken =
            MakeToken("long-expired-token", DateTimeOffset.UtcNow.AddHours(-2));
        await _refreshTokensRepository.AddToken(longExpiredToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(TimeSpan.FromHours(1));

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("long-expired-token");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_OnlyRemovesExpiredTokens_DoesNotAffectOthers()
    {
        // Arrange
        RefreshToken expiredToken = MakeToken("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        RefreshToken activeToken = MakeToken("active-token", DateTimeOffset.UtcNow.AddMinutes(30));
        await _refreshTokensRepository.AddToken(expiredToken);
        await _refreshTokensRepository.AddToken(activeToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens();

        // Assert
        (await _refreshTokensRepository.GetToken("expired-token")).Should().BeNull();
        (await _refreshTokensRepository.GetToken("active-token")).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_RemovesUsedOrInvalidatedTokens_WhenAlsoExpired()
    {
        // Arrange
        RefreshToken usedToken = MakeToken("used-token", DateTimeOffset.UtcNow.AddMinutes(-1), used: true);
        RefreshToken invalidatedToken =
            MakeToken("invalidated-token", DateTimeOffset.UtcNow.AddMinutes(-1), invalidated: true);
        await _refreshTokensRepository.AddToken(usedToken);
        await _refreshTokensRepository.AddToken(invalidatedToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens();

        // Assert
        (await _refreshTokensRepository.GetToken("used-token")).Should().BeNull();
        (await _refreshTokensRepository.GetToken("invalidated-token")).Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_DoesNothing_WhenThereAreNoTokens()
    {
        // Act
        Func<Task> act = async () => await _refreshTokensRepository.DeleteExpiredTokens();

        // Assert
        await act.Should().NotThrowAsync();
    }
}