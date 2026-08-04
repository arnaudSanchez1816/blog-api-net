using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Seeding;
using BlogApi.Services.Auth;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        return services;
    }

    public static async Task<WebApplication> DoDatabaseMigration(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        return app;
    }

    public static async Task<DatabaseSeedingResult> DoDatabaseSeeding(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return new DatabaseSeedingResult();
        }

        using IServiceScope scope = app.Services.CreateScope();
        IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        UserManager<BlogUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<BlogUser>>();

        const string adminEmail = "admin@email.com";
        const string adminPassword = "AdminPassword123";
        BlogUser? admin = await userManager.FindByEmailAsync(adminEmail);
        AuthenticationResult authenticationResult;
        if (admin is null)
        {
            authenticationResult =
                await authService.Register("Administrator", adminEmail, adminPassword);
            if (!authenticationResult.Success)
            {
                throw new InvalidOperationException("Failed to seed default admin user");
            }
        }
        else
        {
            authenticationResult = await authService.Login(adminEmail, adminPassword);
            if (!authenticationResult.Success)
            {
                throw new InvalidOperationException("Failed to login with default admin user");
            }
        }


        return new DatabaseSeedingResult
        {
            DevAccessToken = authenticationResult.AccessToken,
            DevRefreshToken = authenticationResult.RefreshToken
        };
    }
}