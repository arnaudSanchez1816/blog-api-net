using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Repositories.RefreshTokens;

public class RefreshTokenRepository : IRefreshTokensRepository
{
    private readonly DataContext _context;

    public RefreshTokenRepository(DataContext context)
    {
        _context = context;
    }

    public async Task AddToken(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken?> GetToken(string token, bool asNoTracking = false, CancellationToken ct = default)
    {
        IQueryable<RefreshToken> tokenQuery = _context.RefreshTokens.Include(x => x.ReplacedByToken);
        if (asNoTracking)
        {
            tokenQuery = tokenQuery.AsNoTracking();
        }

        return await tokenQuery.SingleOrDefaultAsync(x => x.Token == token, ct);
    }

    public async Task UpdateToken(RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RotateToken(RefreshToken usedToken, RefreshToken newToken, CancellationToken ct = default)
    {
        // Update the used token and add the new one in a single transaction
        _context.RefreshTokens.Update(usedToken);
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default, CancellationToken ct = default)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - expiredForAtLeast;

        await _context.RefreshTokens.Where(t => t.ExpirationDate < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}