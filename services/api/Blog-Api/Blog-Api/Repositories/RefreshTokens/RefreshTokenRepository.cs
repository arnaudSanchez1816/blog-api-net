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

    public async Task<RefreshToken?> GetToken(string token)
    {
        return await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == token);
    }

    public async Task UpdateToken(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredTokens(TimeSpan expiredForAtLeast = default)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - expiredForAtLeast;

        await _context.RefreshTokens.Where(t => t.ExpirationDate < cutoff)
            .ExecuteDeleteAsync();
    }
}