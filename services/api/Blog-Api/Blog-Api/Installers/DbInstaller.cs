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
using Microsoft.Extensions.Options;

namespace BlogApi.Installers;

public static class DbInstaller
{
    public static IServiceCollection InstallDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")).UseExceptionProcessor();
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
        ILogger<DataContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<DataContext>>();
        RoleManager<BlogRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<BlogRole>>();
        IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        UserManager<BlogUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<BlogUser>>();
        DatabaseSeedingOptions databaseSeedingOptions =
            app.Services.GetRequiredService<IOptions<DatabaseSeedingOptions>>().Value;

        logger.LogInformation("Seeding database...");

        // Roles & permissions
        await CreateRoleWithClaims(roleManager, Roles.Admin, Roles.Permissions.AdminPermissions);
        await CreateRoleWithClaims(roleManager, Roles.Moderator, Roles.Permissions.ModeratorPermissions);
        await CreateRoleWithClaims(roleManager, Roles.User, Roles.Permissions.RegisteredUserPermissions);

        // Users
        BlogUser adminUser = await CreateAdminUser(databaseSeedingOptions, userManager);

        // Login with admin user
        AuthenticationResult authenticationResult = await AuthenticateAdminUser(authService, databaseSeedingOptions);

        logger.LogInformation("Database seeding done!");

        return new DatabaseSeedingResult
        {
            DevAccessToken = authenticationResult.AccessToken,
            DevRefreshToken = authenticationResult.RefreshToken
        };
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

    private static async Task CreateRoleWithClaims(RoleManager<BlogRole> roleManager, string roleName,
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
}