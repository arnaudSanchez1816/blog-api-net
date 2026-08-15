using BlogApi.Contracts.Health;
using BlogApi.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace BlogApi.Installers;

public static class HealthChecksInstaller
{
    public static IServiceCollection InstallHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<DataContext>();

        return services;
    }

    public static WebApplication InstallHealthChecks(this WebApplication app)
    {
        app.UseHealthChecks("/health",
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    HealthCheckResponse response = new HealthCheckResponse
                    {
                        Status = report.Status.ToString(),
                        Checks = report.Entries.Select(x => new HealthCheck
                            {
                                Status = x.Value.Status.ToString(),
                                Component = x.Key,
                                Description = x.Value.Description?.ToString() ?? ""
                            })
                            .ToList(),
                        Duration = report.TotalDuration
                    };
                    await context.Response.WriteAsJsonAsync(response);
                }
            });

        return app;
    }
}