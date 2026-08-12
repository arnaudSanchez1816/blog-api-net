using AwesomeAssertions;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration.Repositories;

[Collection(nameof(TestsCollection))]
public class RefreshTokensRepositoryTests : IntegrationTestBase
{
    private DataContext _context = null!;
    private IRefreshTokensRepository _refreshTokensRepository = null!;
    private BlogUser _user = null!;

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

    #region GetToken

    [Fact]
    public async Task GetToken_IncludeReplacedByToken_WhenTokenIsFetched()
    {
        // Arrange
        RefreshToken token = MakeToken("token", DateTimeOffset.UtcNow.AddMinutes(1));
        await _refreshTokensRepository.AddToken(token);
        RefreshToken replacementToken = MakeToken("replacement-token", DateTimeOffset.UtcNow.AddMinutes(1));
        token.ReplacedByToken = replacementToken;
        await _refreshTokensRepository.RotateToken(token, replacementToken);

        // Act
        RefreshToken? fetchedToken = await _refreshTokensRepository.GetToken(token.Token);

        // Assert
        fetchedToken.Should().NotBeNull();
        fetchedToken.ReplacedByToken.Should().Be(replacementToken);
    }

    #endregion

    #region DeleteExpiredTokens

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
        RefreshToken usedToken = MakeToken("used-token", DateTimeOffset.UtcNow.AddMinutes(-1), true);
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

    #endregion

    #region RotateToken

    [Fact]
    public async Task RotateToken_Should_UpdateUsedTokenAndAddNewToken()
    {
        // Arrange
        RefreshToken token = MakeToken("token", DateTimeOffset.UtcNow.AddMinutes(1));
        await _refreshTokensRepository.AddToken(token);
        RefreshToken replacementToken = MakeToken("replacement-token", DateTimeOffset.UtcNow.AddMinutes(1));
        token.ReplacedByToken = replacementToken;
        await _refreshTokensRepository.RotateToken(token, replacementToken);

        // Act
        RefreshToken? updatedToken = await _refreshTokensRepository.GetToken(token.Token);
        RefreshToken? addedToken = await _refreshTokensRepository.GetToken(replacementToken.Token);

        // Assert
        updatedToken.Should().NotBeNull();
        updatedToken.ReplacedByToken.Should().Be(replacementToken);
        addedToken.Should().NotBeNull();
    }

    [Fact]
    public async Task RotateToken_ThrowsConcurrencyException_WhenTwoScopesRotateSameTokenSimultaneously()
    {
        // Arrange
        RefreshToken token = MakeToken("token", DateTimeOffset.UtcNow.AddMinutes(1));
        await _refreshTokensRepository.AddToken(token);

        IServiceScopeFactory scopeFactory = GetRequiredService<IServiceScopeFactory>();

        await using AsyncServiceScope winnerScope = scopeFactory.CreateAsyncScope();
        DataContext winnerContext = winnerScope.ServiceProvider.GetRequiredService<DataContext>();
        IRefreshTokensRepository winnerRepository =
            winnerScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();
        RefreshToken winnerToken =
            await winnerContext.RefreshTokens.SingleAsync(x => x.Token == "token",
                TestContext.Current.CancellationToken);
        RefreshToken winnerChild = MakeToken("winner-child", DateTimeOffset.UtcNow.AddMinutes(1));

        await using AsyncServiceScope loserScope = scopeFactory.CreateAsyncScope();
        DataContext loserContext = loserScope.ServiceProvider.GetRequiredService<DataContext>();
        IRefreshTokensRepository loserRepository =
            loserScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();
        RefreshToken loserToken =
            await loserContext.RefreshTokens.SingleAsync(x => x.Token == "token",
                TestContext.Current.CancellationToken);
        RefreshToken loserChild = MakeToken("loser-child", DateTimeOffset.UtcNow.AddMinutes(1));

        winnerToken.Used = true;
        winnerToken.ReplacedByToken = winnerChild;
        loserToken.Used = true;
        loserToken.ReplacedByToken = loserChild;

        // Act
        await winnerRepository.RotateToken(winnerToken, winnerChild);

        Func<Task> loserAct = async () => await loserRepository.RotateToken(loserToken, loserChild);

        // Assert
        await loserAct.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Use a fresh scope to verify: the original _context/_refreshTokensRepository already tracks "token"
        // in memory (from AddToken above), and EF's identity map won't overwrite tracked scalar/FK values from
        // a re-query, so re-fetching through it would return stale data instead of what's actually in Postgres.
        await using AsyncServiceScope verificationScope = scopeFactory.CreateAsyncScope();
        IRefreshTokensRepository verificationRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();

        RefreshToken? refetchedToken = await verificationRepository.GetToken("token");
        refetchedToken.Should().NotBeNull();
        refetchedToken.ReplacedByToken.Should().NotBeNull();
        refetchedToken.ReplacedByToken!.Token.Should().Be("winner-child");

        RefreshToken? orphanLoserChild = await verificationRepository.GetToken("loser-child");
        orphanLoserChild.Should().BeNull();
    }

    #endregion
}