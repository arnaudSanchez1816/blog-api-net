using BlogApi.Data;
using BlogApi.Domain;

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
}