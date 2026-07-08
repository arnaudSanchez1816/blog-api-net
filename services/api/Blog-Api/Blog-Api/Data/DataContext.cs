using BlogApi.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

public class DataContext : IdentityDbContext<BlogUser, BlogRole, Guid>
{
    public DataContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
    }
    
    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<BlogUser>(user =>
        {
            user.Ignore(u => u.EmailConfirmed);
            user.Ignore(u => u.AccessFailedCount);
            user.Ignore(u => u.ConcurrencyStamp);
            user.Ignore(u => u.LockoutEnabled);
            user.Ignore(u => u.LockoutEnd);
            user.Ignore(u => u.PhoneNumber);
            user.Ignore(u => u.PhoneNumberConfirmed);
            user.Ignore(u => u.SecurityStamp);
            user.Ignore(u => u.TwoFactorEnabled);
        });
    }
}