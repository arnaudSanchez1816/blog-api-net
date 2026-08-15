using System.Threading.RateLimiting;
using BlogApi.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BlogApi.Installers;

public static class RateLimiterInstaller
{
    public const string AuthRateLimiterPolicy = "authLimiter";

    public static IServiceCollection InstallRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppRateLimiterOptions>()
            .BindConfiguration(AppRateLimiterOptions.ConfigurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter();

        return services;
    }

    private static async ValueTask OnRateLimitExceeded(OnRejectedContext context, CancellationToken token)
    {
        IProblemDetailsService problemDetailsService =
            context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many request",
            Detail = "Too many requests. Please try again later."
        };
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = $"{retryAfter.TotalSeconds}";
            problemDetails.Detail = $"Too many requests. Please retry after {retryAfter.TotalSeconds} seconds.";
        }

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = problemDetails
        });
    }

    public static WebApplication InstallRateLimiter(this WebApplication app)
    {
        AppRateLimiterOptions initialRateLimiterOptions =
            app.Services.GetRequiredService<IOptions<AppRateLimiterOptions>>().Value;

        if (!initialRateLimiterOptions.Enabled)
        {
            return app;
        }

        RateLimiterOptions limiterOptions = new RateLimiterOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            OnRejected = OnRateLimitExceeded,
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                AppRateLimiterOptions options = context.RequestServices
                    .GetRequiredService<IOptionsMonitor<AppRateLimiterOptions>>()
                    .CurrentValue;

                string incomingIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(incomingIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalPermitLimit,
                        AutoReplenishment = true,
                        Window = TimeSpan.FromMinutes(1)
                    });
            })
        };

        limiterOptions.AddPolicy(AuthRateLimiterPolicy,
            context =>
            {
                AppRateLimiterOptions options = context.RequestServices
                    .GetRequiredService<IOptionsMonitor<AppRateLimiterOptions>>()
                    .CurrentValue;

                string incomingIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(incomingIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.AuthPermitLimit,
                        AutoReplenishment = true,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

        app.UseRateLimiter(limiterOptions);

        return app;
    }
}