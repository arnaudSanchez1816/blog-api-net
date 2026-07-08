using BlogApi.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Installers;

public static class DbInstaller
{
    public static IServiceCollection InstallDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static async Task<WebApplication> DoDatabaseMigration(this WebApplication app)
    {
        return app;
    }

    public static async Task<WebApplication> DoDatabaseSeeding(this WebApplication app)
    {
        return app;
    }
}