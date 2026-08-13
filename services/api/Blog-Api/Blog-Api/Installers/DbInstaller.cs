using System.Reflection;
using System.Security.Claims;
using BlogApi.Authorization;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Options;
using BlogApi.Seeding;
using BlogApi.Services.Auth;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace BlogApi.Installers;

public static class DbInstaller
{
    public static IServiceCollection InstallDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseExceptionProcessor()
                .UseAsyncSeeding(SeedDatabaseInternal)
                .UseSeeding((context, b) =>
                {
                    SeedDatabaseInternal(context, b, CancellationToken.None).GetAwaiter().GetResult();
                });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddOptions<DatabaseSeedingOptions>()
            .BindConfiguration(DatabaseSeedingOptions.ConfigurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static async Task<WebApplication> MigrateDatabase(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        return app;
    }

    public static async Task<DatabaseSeedingResult> SeedDatabase(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return new DatabaseSeedingResult();
        }

        using IServiceScope scope = app.Services.CreateScope();
        DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();
        ILogger<DataContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<DataContext>>();
        IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        DatabaseSeedingOptions databaseSeedingOptions =
            app.Services.GetRequiredService<IOptions<DatabaseSeedingOptions>>().Value;

        logger.LogInformation("Seeding database...");

        // Ensure created calls UseAsyncSeeding 
        await context.Database.EnsureCreatedAsync();

        logger.LogInformation("Database seeding done!");

        logger.LogInformation("Login with Admin user");
        // Login with admin user
        AuthenticationResult authenticationResult = await AuthenticateAdminUser(authService, databaseSeedingOptions);

        return new DatabaseSeedingResult
        {
            DevAccessToken = authenticationResult.AccessToken,
            DevRefreshToken = authenticationResult.RefreshToken
        };
    }

    private static async Task SeedDatabaseInternal(DbContext context, bool didMigrate, CancellationToken ct)
    {
        RoleManager<BlogRole> roleManager = context.GetService<RoleManager<BlogRole>>();
        UserManager<BlogUser> userManager = context.GetService<UserManager<BlogUser>>();
        DatabaseSeedingOptions databaseSeedingOptions =
            context.GetService<IOptions<DatabaseSeedingOptions>>().Value;

        // Roles & permissions
        await CreateRoleWithClaims(roleManager, Roles.Admin, Roles.Permissions.AdminPermissions);
        await CreateRoleWithClaims(roleManager, Roles.Moderator, Roles.Permissions.ModeratorPermissions);
        await CreateRoleWithClaims(roleManager, Roles.User, Roles.Permissions.RegisteredUserPermissions);

        // Users
        BlogUser adminUser = await CreateAdminUser(databaseSeedingOptions, userManager);

        // Tags
        Tag jsTag = await CreateTag(context, "Javascript", "js");
        Tag dotNetTag = await CreateTag(context, "ASP.NET Core", "asp-net-core");

        // Posts
        if (!await context.Set<Post>().AnyAsync(ct))
        {
            await CreateDemoBlogPosts(context, ct, adminUser, jsTag, dotNetTag);
        }
    }

    private static async Task CreateDemoBlogPosts(DbContext context, CancellationToken ct, BlogUser adminUser,
        Tag jsTag,
        Tag dotNetTag)
    {
        Post markdownDemoPost = new Post
        {
            Title = "Markdown support demonstration",
            Slug = "markdown-support-demonstration",
            Description = "This post display examples of supported markdown features.",
            Body = await ReadResourceFile("BlogApi.Resources.markdown_test.md", ct) ??
                   "# Markdown support demonstration",
            AuthorId = adminUser.Id,
            ReadingTime = 5,
            PublishedAt = DateTimeOffset.UtcNow,
            Tags = { jsTag }
        };
        context.Set<Post>().Add(markdownDemoPost);

        Post loremPost = new Post
        {
            Title = "Munera parabat turis",
            Slug = "munera-parabat-turis",
            Description = "Lorem markdownum. Cura spumis despexitque tegi Tartara",
            Body = await ReadResourceFile("BlogApi.Resources.markdown_lorem1.md", ct) ??
                   "Lorem markdownum. Cura spumis despexitque tegi Tartara",
            AuthorId = adminUser.Id,
            ReadingTime = 10,
            PublishedAt = DateTimeOffset.UtcNow,
            Tags = { jsTag, dotNetTag }
        };
        context.Set<Post>().Add(loremPost);

        Post draftPost = new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            Description = "Draft post, this post is not visible except to you!",
            Body = "# This is an unpublished post !\nYou can edit my content or publish me right away!",
            AuthorId = adminUser.Id,
            ReadingTime = 1
        };
        context.Set<Post>().Add(draftPost);

        await context.SaveChangesAsync(ct);

        // Comments
        Comment loremPostComment = new Comment
        {
            Username = "Friendly user",
            Body = "Very interesting, thank you !",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(5),
            PostId = loremPost.Id
        };
        context.Set<Comment>().Add(loremPostComment);

        await context.SaveChangesAsync(ct);
    }

    private static async Task<string?> ReadResourceFile(string resourceName, CancellationToken ct = default)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        await using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using StreamReader reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    private static async Task<AuthenticationResult> AuthenticateAdminUser(IAuthService authService,
        DatabaseSeedingOptions databaseSeedingOptions)
    {
        AuthenticationResult authenticationResult =
            await authService.Login(databaseSeedingOptions.AdminEmail, databaseSeedingOptions.AdminPassword);
        if (!authenticationResult.Success)
        {
            throw new InvalidOperationException("Failed to login with default admin user");
        }

        return authenticationResult;
    }

    private static async Task<BlogUser> CreateAdminUser(
        DatabaseSeedingOptions databaseSeedingOptions, UserManager<BlogUser> userManager)
    {
        string adminEmail = databaseSeedingOptions.AdminEmail;
        string adminPassword = databaseSeedingOptions.AdminPassword;
        string adminName = databaseSeedingOptions.AdminName;
        BlogUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            await userManager.CreateAsync(admin = new BlogUser
                {
                    DisplayName = adminName,
                    Email = adminEmail,
                    UserName = adminEmail
                },
                adminPassword);
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        return admin;
    }

    private static async Task CreateRoleWithClaims(RoleManager<BlogRole> roleManager,
        string roleName,
        IReadOnlyCollection<string> rolePermissions)
    {
        BlogRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            await roleManager.CreateAsync(role = new BlogRole { Name = roleName });
            IReadOnlyCollection<Claim> permissionClaims =
                rolePermissions.Select(p => new Claim(CustomClaimTypes.Permission, p)).ToList();
            foreach (Claim claim in permissionClaims)
            {
                await roleManager.AddClaimAsync(role, claim);
            }
        }
    }

    private static async Task<Tag> CreateTag(DbContext context, string name, string slug)
    {
        Tag? tag = await context.Set<Tag>().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tag == null)
        {
            tag = new Tag
            {
                Name = name,
                Slug = slug
            };
            context.Set<Tag>().Add(tag);
            await context.SaveChangesAsync();
        }

        return tag;
    }
}