using AwesomeAssertions;
using BlogApi.Options;
using BlogApi.Repositories.RefreshTokens;
using BlogApi.Services.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlogApi.Unit.Services;

public class RefreshTokensCleanupServiceTests : IDisposable
{
    private readonly Mock<IRefreshTokensRepository> _refreshTokensRepository;
    private readonly Mock<IServiceScope> _scope;
    private readonly Mock<IServiceScopeFactory> _scopeFactory;

    public RefreshTokensCleanupServiceTests()
    {
        _refreshTokensRepository = new Mock<IRefreshTokensRepository>();

        Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IRefreshTokensRepository)))
            .Returns(_refreshTokensRepository.Object);

        _scope = new Mock<IServiceScope>();
        _scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        _scopeFactory = new Mock<IServiceScopeFactory>();
        _scopeFactory.Setup(x => x.CreateScope()).Returns(_scope.Object);
    }

    public void Dispose()
    {
        _refreshTokensRepository.Reset();
        _scope.Reset();
        _scopeFactory.Reset();
    }

    private RefreshTokensCleanupService CreateService(TimeSpan interval, TimeSpan? buffer = null)
    {
        AppAuthenticationOptions options = new AppAuthenticationOptions
        {
            JwtAccessSecret = new string('a', 32),
            JwtIssuerUri = new Uri("https://issuer.example.com"),
            JwtAudienceUri = new Uri("https://audience.example.com"),
            RefreshTokensCleanupInterval = interval,
            RefreshTokensExpirationBuffer = buffer ?? TimeSpan.Zero
        };

        return new RefreshTokensCleanupService(_scopeFactory.Object,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CallsDeleteExpiredTokens_WithConfiguredBuffer()
    {
        TimeSpan buffer = TimeSpan.FromMinutes(5);
        RefreshTokensCleanupService service = CreateService(TimeSpan.FromMilliseconds(20), buffer);

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => _refreshTokensRepository.Invocations.Count > 0, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        _refreshTokensRepository.Verify(x => x.DeleteExpiredTokens(buffer, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CallsDeleteExpiredTokens_OnEachTick()
    {
        RefreshTokensCleanupService service = CreateService(TimeSpan.FromMilliseconds(20));

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => _refreshTokensRepository.Invocations.Count >= 3, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        _refreshTokensRepository.Invocations.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesNewScope_ForEachTick()
    {
        RefreshTokensCleanupService service = CreateService(TimeSpan.FromMilliseconds(20));

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => _scopeFactory.Invocations.Count >= 2, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        _scopeFactory.Verify(x => x.CreateScope(), Times.AtLeast(2));
        _scope.Verify(x => x.Dispose(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_StopsCallingRepository_AfterStopAsync()
    {
        RefreshTokensCleanupService service = CreateService(TimeSpan.FromMilliseconds(20));

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => _refreshTokensRepository.Invocations.Count > 0, TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        int countAfterStop = _refreshTokensRepository.Invocations.Count;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _refreshTokensRepository.Invocations.Count.Should().Be(countAfterStop);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCallRepository_BeforeFirstIntervalElapses()
    {
        RefreshTokensCleanupService service = CreateService(TimeSpan.FromSeconds(10));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await service.StopAsync(CancellationToken.None);

        _refreshTokensRepository.Invocations.Should().BeEmpty();
    }
}