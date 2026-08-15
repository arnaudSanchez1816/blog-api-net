using Microsoft.AspNetCore.HttpOverrides;

namespace BlogApi.Installers;

public static class ForwardedHeadersInstaller
{
    // This fixes Scalar endpoints calls not working on the live version of the api due to https
    public static IServiceCollection InstallForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Dokploy's reverse proxy runs on an internal Docker network with an address
            // that isn't known ahead of time, so the usual KnownProxies/KnownNetworks
            // allowlist can't be used here.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static WebApplication InstallForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();

        return app;
    }
}