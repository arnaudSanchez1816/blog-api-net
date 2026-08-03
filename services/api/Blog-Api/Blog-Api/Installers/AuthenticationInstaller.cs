using System.Text;
using BlogApi.Options;
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
            .GetRequiredSection(AppAuthenticationOptions.ConfigurationSection).Get<AppAuthenticationOptions>();
        if (authenticationOptions is null)
        {
            throw new InvalidOperationException(
                $"Valid {AppAuthenticationOptions.ConfigurationSection} section is required.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
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
                ValidIssuer = authenticationOptions.JwtIssuerUri.ToString()
            };
            x.SaveToken = true;
        });

        return services;
    }
}