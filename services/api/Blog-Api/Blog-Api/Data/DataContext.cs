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

        IgnoreUnusedIdentityUserProperties(builder);
    }

    private static void IgnoreUnusedIdentityUserProperties(ModelBuilder builder)
    {
        builder.Entity<BlogUser>().Ignore(user => user.EmailConfirmed);
        builder.Entity<BlogUser>().Ignore(user => user.AccessFailedCount);
        builder.Entity<BlogUser>().Ignore(user => user.ConcurrencyStamp);
        builder.Entity<BlogUser>().Ignore(user => user.LockoutEnabled);
        builder.Entity<BlogUser>().Ignore(user => user.LockoutEnd);
        builder.Entity<BlogUser>().Ignore(user => user.PhoneNumber);
        builder.Entity<BlogUser>().Ignore(user => user.PhoneNumberConfirmed);
        builder.Entity<BlogUser>().Ignore(user => user.SecurityStamp);
        builder.Entity<BlogUser>().Ignore(user => user.TwoFactorEnabled);
    }
}