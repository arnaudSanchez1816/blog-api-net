using BlogApi.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

public class DataContext : IdentityDbContext<BlogUser, BlogRole, Guid>
{
    public DbSet<Post> Posts { get; set; }

    public DataContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
        ConfigurePost(builder);
    }

    private static void ConfigurePost(ModelBuilder builder)
    {
        builder.Entity<Post>(post =>
        {
            post.Property(p => p.Title).HasMaxLength(300);
            post.Property(p => p.Slug).HasMaxLength(220);
            post.HasIndex(p => p.Slug).IsUnique();
        });
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