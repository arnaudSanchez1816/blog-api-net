using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Installers;

public static class IdentityInstaller
{
    public static IServiceCollection InstallIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>().BindConfiguration(nameof(IdentityOptions)).ValidateOnStart();
        services.AddIdentityCore<BlogUser>().AddRoles<BlogRole>().AddEntityFrameworkStores<DataContext>();

        services.AddOptions<AppDataProtectionOptions>()
            .BindConfiguration(AppDataProtectionOptions.ConfigurationSection);

        // Set up data protection, this is not useful for this project, but it fixes a warning when building the application
        AppDataProtectionOptions? appDataProtectionOptions = configuration
            .GetSection(AppDataProtectionOptions.ConfigurationSection).Get<AppDataProtectionOptions>();
        string dataProtectionKeysPath = appDataProtectionOptions?.KeysPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "keys");
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("BlogApi");

        return services;
    }
}