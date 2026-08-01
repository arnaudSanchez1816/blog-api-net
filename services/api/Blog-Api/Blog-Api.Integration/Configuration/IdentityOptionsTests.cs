using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BlogApi.Integration.Configuration;

[Collection(nameof(TestsCollection))]
public class IdentityOptionsTests : IntegrationTestBase
{
    private IOptions<IdentityOptions> _identityOptions = null!;

    public IdentityOptionsTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override Task OnInitializeAsync()
    {
        _identityOptions = GetRequiredService<IOptions<IdentityOptions>>();
        return Task.CompletedTask;
    }

    /// Independently binds the real appsettings.json to a fresh IdentityOptions instance, so this
    /// test fails only when DI-bound options diverge from the file (e.g. a property name typo like
    /// RequiredDigit vs RequireDigit).
    private static IdentityOptions BindFromAppSettings([CallerFilePath] string testFilePath = "")
    {
        string appSettingsPath = Path.Combine(
            Path.GetDirectoryName(testFilePath)!, "..", "..", "Blog-Api", "appsettings.json");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath(appSettingsPath))
            .Build();

        IdentityOptions options = new IdentityOptions();
        configuration.GetSection(nameof(IdentityOptions)).Bind(options);
        return options;
    }

    [Fact]
    public void IdentityOptions_BindsPasswordSettings_MatchingAppSettingsJson()
    {
        IdentityOptions expected = BindFromAppSettings();

        PasswordOptions password = _identityOptions.Value.Password;

        password.RequiredLength.Should().Be(expected.Password.RequiredLength);
        password.RequireLowercase.Should().Be(expected.Password.RequireLowercase);
        password.RequireUppercase.Should().Be(expected.Password.RequireUppercase);
        password.RequireDigit.Should().Be(expected.Password.RequireDigit);
        password.RequireNonAlphanumeric.Should().Be(expected.Password.RequireNonAlphanumeric);
    }

    [Fact]
    public void IdentityOptions_BindsUserSettings_MatchingAppSettingsJson()
    {
        IdentityOptions expected = BindFromAppSettings();

        UserOptions user = _identityOptions.Value.User;

        user.RequireUniqueEmail.Should().Be(expected.User.RequireUniqueEmail);
    }
}