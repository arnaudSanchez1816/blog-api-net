using System.Text;
using BlogApi.Authentication;
using BlogApi.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BlogApi.Installers;

public static class AuthenticationInstaller
{
    public static IServiceCollection InstallAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AppAuthenticationOptions>()
            .BindConfiguration(AppAuthenticationOptions.ConfigurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();


        AppAuthenticationOptions? authenticationOptions = configuration
            .GetRequiredSection(AppAuthenticationOptions.ConfigurationSection)
            .Get<AppAuthenticationOptions>();
        if (authenticationOptions is null)
        {
            throw new InvalidOperationException(
                $"Valid {AppAuthenticationOptions.ConfigurationSection} section is required.");
        }

        AuthenticationBuilder authenticationBuilder =
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        authenticationBuilder.AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationOptions.JwtAccessSecret)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudience = authenticationOptions.JwtAudienceUri.ToString(),
                ValidIssuer = authenticationOptions.JwtIssuerUri.ToString(),
                ClockSkew = TimeSpan.Zero
            };
            x.SaveToken = true;
        });
        // Override Token Validation LifetimeValidator to use the injected TimeProvider (System.TimeProvider in production), see ServicesInstaller
        // This allows us to override the TimeProvider during tests for token expiration tests
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<TimeProvider>((options, timeProvider) =>
                options.TokenValidationParameters.LifetimeValidator =
                    (notBefore, expires, _, parameters) => ValidateLifetime(notBefore,
                        expires,
                        parameters,
                        timeProvider));

        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, RefreshTokenAuthenticationHandler>(
            RefreshTokenAuthDefaults.RefreshTokenScheme,
            "Refresh Token Authentication",
            _ =>
            {
            });

        return services;
    }

    public static WebApplication InstallAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseMiddleware<InvalidBearerTokenMiddleware>();
        return app;
    }

    private static bool ValidateLifetime(DateTime? notBefore, DateTime? expires,
        TokenValidationParameters parameters, TimeProvider timeProvider)
    {
        if (expires is null)
        {
            return !parameters.RequireExpirationTime;
        }

        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (notBefore is not null)
        {
            if (notBefore > expires)
            {
                return false;
            }

            if (utcNow < notBefore.Value.Subtract(parameters.ClockSkew))
            {
                return false;
            }
        }

        return utcNow <= expires.Value.Add(parameters.ClockSkew);
    }
}