using BlogApi.Options;
using BlogApi.Repositories.RefreshTokens;
using Microsoft.Extensions.Options;

namespace BlogApi.Services.Tokens;

public class RefreshTokensCleanupService : BackgroundService
{
    private readonly AppAuthenticationOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public RefreshTokensCleanupService(IServiceScopeFactory scopeFactory, IOptions<AppAuthenticationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(_options.RefreshTokensCleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            IRefreshTokensRepository refreshTokensRepository =
                scope.ServiceProvider.GetRequiredService<IRefreshTokensRepository>();
            await refreshTokensRepository.DeleteExpiredTokens(_options.RefreshTokensExpirationBuffer);
        }
    }
}