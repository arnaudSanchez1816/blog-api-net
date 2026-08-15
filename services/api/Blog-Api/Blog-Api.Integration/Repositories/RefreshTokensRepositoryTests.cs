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
        await _refreshTokensRepository.AddToken(token, ct: TestContext.Current.CancellationToken);
        RefreshToken replacementToken = MakeToken("replacement-token", DateTimeOffset.UtcNow.AddMinutes(1));
        token.ReplacedByToken = replacementToken;
        await _refreshTokensRepository.RotateToken(token, replacementToken, ct: TestContext.Current.CancellationToken);

        // Act
        RefreshToken? fetchedToken = await _refreshTokensRepository.GetToken(token.Token, ct: TestContext.Current.CancellationToken);

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
        await _refreshTokensRepository.AddToken(expiredToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(ct: TestContext.Current.CancellationToken);

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("expired-token", ct: TestContext.Current.CancellationToken);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_KeepsActiveToken_WhenNoBufferIsGiven()
    {
        // Arrange
        RefreshToken activeToken = MakeToken("active-token", DateTimeOffset.UtcNow.AddMinutes(30));
        await _refreshTokensRepository.AddToken(activeToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(ct: TestContext.Current.CancellationToken);

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("active-token", ct: TestContext.Current.CancellationToken);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_KeepsRecentlyExpiredToken_WhenWithinBuffer()
    {
        // Arrange
        RefreshToken recentlyExpiredToken =
            MakeToken("recently-expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        await _refreshTokensRepository.AddToken(recentlyExpiredToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(TimeSpan.FromHours(1), ct: TestContext.Current.CancellationToken);

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("recently-expired-token", ct: TestContext.Current.CancellationToken);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_RemovesToken_WhenExpiredForLongerThanBuffer()
    {
        // Arrange
        RefreshToken longExpiredToken =
            MakeToken("long-expired-token", DateTimeOffset.UtcNow.AddHours(-2));
        await _refreshTokensRepository.AddToken(longExpiredToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(TimeSpan.FromHours(1), ct: TestContext.Current.CancellationToken);

        // Assert
        RefreshToken? found = await _refreshTokensRepository.GetToken("long-expired-token", ct: TestContext.Current.CancellationToken);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_OnlyRemovesExpiredTokens_DoesNotAffectOthers()
    {
        // Arrange
        RefreshToken expiredToken = MakeToken("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        RefreshToken activeToken = MakeToken("active-token", DateTimeOffset.UtcNow.AddMinutes(30));
        await _refreshTokensRepository.AddToken(expiredToken, ct: TestContext.Current.CancellationToken);
        await _refreshTokensRepository.AddToken(activeToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(ct: TestContext.Current.CancellationToken);

        // Assert
        (await _refreshTokensRepository.GetToken("expired-token", ct: TestContext.Current.CancellationToken)).Should().BeNull();
        (await _refreshTokensRepository.GetToken("active-token", ct: TestContext.Current.CancellationToken)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExpiredTokens_RemovesUsedOrInvalidatedTokens_WhenAlsoExpired()
    {
        // Arrange
        RefreshToken usedToken = MakeToken("used-token", DateTimeOffset.UtcNow.AddMinutes(-1), true);
        RefreshToken invalidatedToken =
            MakeToken("invalidated-token", DateTimeOffset.UtcNow.AddMinutes(-1), invalidated: true);
        await _refreshTokensRepository.AddToken(usedToken, ct: TestContext.Current.CancellationToken);
        await _refreshTokensRepository.AddToken(invalidatedToken, ct: TestContext.Current.CancellationToken);

        // Act
        await _refreshTokensRepository.DeleteExpiredTokens(ct: TestContext.Current.CancellationToken);

        // Assert
        (await _refreshTokensRepository.GetToken("used-token", ct: TestContext.Current.CancellationToken)).Should().BeNull();
        (await _refreshTokensRepository.GetToken("invalidated-token", ct: TestContext.Current.CancellationToken)).Should().BeNull();
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
        await _refreshTokensRepository.AddToken(token, ct: TestContext.Current.CancellationToken);
        RefreshToken replacementToken = MakeToken("replacement-token", DateTimeOffset.UtcNow.AddMinutes(1));
        token.ReplacedByToken = replacementToken;
        await _refreshTokensRepository.RotateToken(token, replacementToken, ct: TestContext.Current.CancellationToken);

        // Act
        RefreshToken? updatedToken = await _refreshTokensRepository.GetToken(token.Token, ct: TestContext.Current.CancellationToken);
        RefreshToken? addedToken = await _refreshTokensRepository.GetToken(replacementToken.Token, ct: TestContext.Current.CancellationToken);

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
        await _refreshTokensRepository.AddToken(token, ct: TestContext.Current.CancellationToken);

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
        await winnerRepository.RotateToken(winnerToken, winnerChild, ct: TestContext.Current.CancellationToken);

        Func<Task> loserAct = async () => await loserRepository.RotateToken(loserToken, loserChild);

        // Assert
        await loserAct.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Use a fresh scope to verify: the original _context/_refreshTokensRepository already tracks "token"
        // in memory (from AddToken above), and EF's identity map won't overwrite tracked scalar/FK values from
        // a re-query, so re-fetching through it would return stale data instead of what's actually in Postgres.
        await using AsyncServiceScope verificationScope = scopeFactory.CreateAsyncScope();
        IRefreshTokensRepository verificationRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();

        RefreshToken? refetchedToken = await verificationRepository.GetToken("token", ct: TestContext.Current.CancellationToken);
        refetchedToken.Should().NotBeNull();
        refetchedToken.ReplacedByToken.Should().NotBeNull();
        refetchedToken.ReplacedByToken!.Token.Should().Be("winner-child");

        RefreshToken? orphanLoserChild = await verificationRepository.GetToken("loser-child", ct: TestContext.Current.CancellationToken);
        orphanLoserChild.Should().BeNull();
    }

    [Fact]
    public async Task GetToken_ReturnsStaleTrackedInstance_WhenCalledOnSameContextAfterFailedRotate()
    {
        // Arrange: reproduces the bug where a concurrency-conflict recovery path re-queried through the same
        // DbContext that still tracked the losing entity's rejected, unsaved mutations. Because that entity
        // is already tracked, EF's identity map hands the same (stale) instance back instead of hitting the
        // database, so the recovery code would see its own rejected child instead of the real winner.
        RefreshToken token = MakeToken("token", DateTimeOffset.UtcNow.AddMinutes(1));
        await _refreshTokensRepository.AddToken(token, ct: TestContext.Current.CancellationToken);

        IServiceScopeFactory scopeFactory = GetRequiredService<IServiceScopeFactory>();

        await using AsyncServiceScope winnerScope = scopeFactory.CreateAsyncScope();
        IRefreshTokensRepository winnerRepository =
            winnerScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();
        DataContext winnerContext = winnerScope.ServiceProvider.GetRequiredService<DataContext>();
        RefreshToken winnerToken =
            await winnerContext.RefreshTokens.SingleAsync(x => x.Token == "token",
                TestContext.Current.CancellationToken);
        RefreshToken winnerChild = MakeToken("winner-child-2", DateTimeOffset.UtcNow.AddMinutes(1));
        winnerToken.Used = true;
        winnerToken.ReplacedByToken = winnerChild;

        await using AsyncServiceScope loserScope = scopeFactory.CreateAsyncScope();
        IRefreshTokensRepository loserRepository =
            loserScope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();
        DataContext loserContext = loserScope.ServiceProvider.GetRequiredService<DataContext>();
        RefreshToken loserToken =
            await loserContext.RefreshTokens.SingleAsync(x => x.Token == "token",
                TestContext.Current.CancellationToken);
        RefreshToken loserChild = MakeToken("loser-child-2", DateTimeOffset.UtcNow.AddMinutes(1));
        loserToken.Used = true;
        loserToken.ReplacedByToken = loserChild;

        // Act
        await winnerRepository.RotateToken(winnerToken, winnerChild, ct: TestContext.Current.CancellationToken);

        Func<Task> loserAct = async () => await loserRepository.RotateToken(loserToken, loserChild);
        await loserAct.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Assert: a tracked re-query on the loser's own context returns its own rejected, never-persisted
        // child instead of the real committed winner.
        RefreshToken? staleReread = await loserRepository.GetToken("token", ct: TestContext.Current.CancellationToken);
        staleReread.Should().NotBeNull();
        staleReread.ReplacedByToken.Should().NotBeNull();
        staleReread.ReplacedByToken!.Token.Should().Be("loser-child-2");

        // A no-tracking read bypasses the identity map and returns the real, committed replacement.
        RefreshToken? freshReread = await loserRepository.GetToken("token", true, ct: TestContext.Current.CancellationToken);
        freshReread.Should().NotBeNull();
        freshReread.ReplacedByToken.Should().NotBeNull();
        freshReread.ReplacedByToken!.Token.Should().Be("winner-child-2");
    }

    #endregion

    [Fact]
    public async Task GetToken_Throws_WhenCancellationIsRequested()
    {
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _refreshTokensRepository.GetToken("token", ct: cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddToken_Throws_WhenCancellationIsRequested()
    {
        RefreshToken token = MakeToken("cancelled-token", DateTimeOffset.UtcNow.AddMinutes(1));
        CancellationToken cancelledToken = new CancellationToken(canceled: true);

        Func<Task> act = async () => await _refreshTokensRepository.AddToken(token, ct: cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}