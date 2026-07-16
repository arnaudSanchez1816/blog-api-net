using System.Diagnostics;
using BlogApi.Exceptions.Handlers;
using Microsoft.AspNetCore.Http.Features;

namespace BlogApi.Installers;

public static class ExceptionHandlingInstaller
{
    public static IServiceCollection InstallExceptionHandlers(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                HttpContext httpContext = context.HttpContext;
                context.ProblemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);
                Activity? activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
            };
        });
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static WebApplication InstallExceptionHandlers(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}