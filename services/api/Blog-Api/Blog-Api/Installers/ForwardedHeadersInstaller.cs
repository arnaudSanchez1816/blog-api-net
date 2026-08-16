using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace BlogApi.Installers;

public static class ForwardedHeadersInstaller
{
    // This fixes Scalar endpoints calls not working on the live version of the api due to https
    public static IServiceCollection InstallForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Add all ipv4 private ranges, this should allow communication with Dokploy reverse proxy inside the docker container
            options.KnownIPNetworks.Add(IPNetwork.Parse("10.0.0.0/8"));
            options.KnownIPNetworks.Add(IPNetwork.Parse("172.16.0.0/12"));
            options.KnownIPNetworks.Add(IPNetwork.Parse("192.168.0.0/16"));
        });

        return services;
    }

    public static WebApplication InstallForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();

        return app;
    }
}