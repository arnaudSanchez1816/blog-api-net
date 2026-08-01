using AwesomeAssertions;
using BlogApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Integration.Configuration;

[Collection(nameof(TestsCollection))]
public class UserManagerPasswordPolicyTests : IntegrationTestBase
{
    private UserManager<BlogUser> _userManager = null!;

    public UserManagerPasswordPolicyTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override Task OnInitializeAsync()
    {
        _userManager = GetRequiredService<UserManager<BlogUser>>();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("WEAK")]
    [InlineData("Weak")]
    [InlineData("weak1")]
    [InlineData("Weak1")]
    [InlineData("weaklongpass")]
    [InlineData("weaklongpass1")]
    public async Task CreateAsync_Fails_WhenPasswordViolatesConfiguredPolicy(string password)
    {
        BlogUser user = new BlogUser
        {
            UserName = "weakpassworduser@example.com",
            Email = "weakpassworduser@example.com",
            DisplayName = "Weak Password User"
        };

        IdentityResult result = await _userManager.CreateAsync(user, password);

        result.Succeeded.Should().BeFalse();
        List<string> errorCodes = result.Errors.Select(e => e.Code).ToList();
        errorCodes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenPasswordSatisfiesConfiguredPolicy()
    {
        BlogUser user = new BlogUser
        {
            UserName = "validpassworduser@example.com",
            Email = "validpassworduser@example.com",
            DisplayName = "Valid Password User"
        };

        IdentityResult result = await _userManager.CreateAsync(user, "ValidPass123");

        result.Succeeded.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}