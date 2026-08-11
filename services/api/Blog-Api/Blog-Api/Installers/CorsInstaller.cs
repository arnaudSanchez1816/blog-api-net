using BlogApi.Options;
using Microsoft.Extensions.Options;

namespace BlogApi.Installers;

public static class CorsInstaller
{
    private const string AllowAllOriginsPolicy = "AllowAll";

    public static IServiceCollection InstallCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsOptions>().BindConfiguration(CorsOptions.ConfigurationSection);

        CorsOptions? corsOptions = configuration
            .GetSection(CorsOptions.ConfigurationSection)
            .Get<CorsOptions>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                List<string> origins = new List<string>();
                if (corsOptions != null)
                {
                    origins.AddRange(corsOptions.AllowedOrigins);
                }

                builder.WithOrigins(origins.ToArray())
                    .AllowAnyHeader()
                    .WithExposedHeaders("Authorization")
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromSeconds(7200));
            });

            options.AddPolicy(AllowAllOriginsPolicy,
                builder =>
                {
                    builder.AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("Authorization")
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(_ => true);
                });
        });

        return services;
    }

    public static WebApplication InstallCors(this WebApplication app)
    {
        CorsOptions corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        if (!app.Environment.IsProduction() && corsOptions.AllowAllOrigins)
        {
            app.UseCors(AllowAllOriginsPolicy);
        }
        else
        {
            app.UseCors();
        }

        return app;
    }
}