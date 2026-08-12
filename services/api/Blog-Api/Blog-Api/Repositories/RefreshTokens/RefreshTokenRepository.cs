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

    public async Task AddToken(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetToken(string token, bool asNoTracking = false)
    {
        IQueryable<RefreshToken> tokenQuery = _context.RefreshTokens.Include(x => x.ReplacedByToken);
        if (asNoTracking)
        {
            tokenQuery = tokenQuery.AsNoTracking();
        }

        return await tokenQuery.SingleOrDefaultAsync(x => x.Token == token);
    }

    public async Task UpdateToken(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task RotateToken(RefreshToken usedToken, RefreshToken newToken)
    {
        // Update the used token and add the new one in a single transaction
        _context.RefreshTokens.Update(usedToken);
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - expiredForAtLeast;

        await _context.RefreshTokens.Where(t => t.ExpirationDate < cutoff)
            .ExecuteDeleteAsync();
    }
}