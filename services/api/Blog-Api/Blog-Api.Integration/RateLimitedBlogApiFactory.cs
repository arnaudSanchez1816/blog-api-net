using BlogApi.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Testing.Platform.Services;

namespace BlogApi.Integration;

public class RateLimitedBlogApiFactory : BlogApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AppRateLimiterOptions.ConfigurationSection}:{nameof(AppRateLimiterOptions.Enabled)}"] = "true"
            });
        });
    }

    public void SetAuthRateLimit(uint limit)
    {
        IConfigurationRoot configuration = (IConfigurationRoot)Services.GetRequiredService<IConfiguration>();
        configuration
                [$"{AppRateLimiterOptions.ConfigurationSection}:{nameof(AppRateLimiterOptions.AuthPermitLimit)}"] =
            limit.ToString();
        configuration.Reload();
    }

    public void SetGlobalRateLimit(uint limit)
    {
        IConfigurationRoot configuration = (IConfigurationRoot)Services.GetRequiredService<IConfiguration>();
        configuration
                [$"{AppRateLimiterOptions.ConfigurationSection}:{nameof(AppRateLimiterOptions.GlobalPermitLimit)}"] =
            limit.ToString();
        configuration.Reload();
    }
}