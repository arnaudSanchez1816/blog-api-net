using System.Security.Claims;
using AwesomeAssertions;
using BlogApi.Domain;
using BlogApi.Services.Auth;
using BlogApi.Services.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BlogApi.Unit.Services;

public class AuthServiceTests : IDisposable
{
    private readonly IAuthService _authService;
    private readonly Mock<RoleManager<BlogRole>> _roleManager;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<UserManager<BlogUser>> _userManager;

    public AuthServiceTests()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        Mock<IUserStore<BlogUser>> userStore = new Mock<IUserStore<BlogUser>>();
        _userManager = new Mock<UserManager<BlogUser>>(userStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        _userManager.Setup(x => x.GetClaimsAsync(It.IsAny<BlogUser>())).ReturnsAsync(new List<Claim>());
        _userManager.Setup(x => x.GetRolesAsync(It.IsAny<BlogUser>())).ReturnsAsync(new List<string>());

        Mock<IRoleStore<BlogRole>> roleStore = new Mock<IRoleStore<BlogRole>>();
        _roleManager = new Mock<RoleManager<BlogRole>>(roleStore.Object, null!, null!, null!, null!);
        _roleManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new BlogRole());
        _roleManager.Setup(x => x.GetClaimsAsync(It.IsAny<BlogRole>()))
            .ReturnsAsync(new List<Claim>());

        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(x => x.GenerateAccessToken(It.IsAny<BlogUser>(), It.IsAny<IReadOnlyCollection<Claim>>()))
            .Returns("access-token");
        _tokensService.Setup(x => x.GenerateAndSaveRefreshToken(It.IsAny<BlogUser>()))
            .ReturnsAsync((BlogUser user) => MakeRefreshToken(user.Id));
        _tokensService.Setup(x => x.CreateRefreshToken(It.IsAny<BlogUser>()))
            .Returns((BlogUser user) => MakeRefreshToken(user.Id));

        _authService = new AuthService(_userManager.Object,
            _tokensService.Object,
            _roleManager.Object,
            _timeProvider);
    }

    public void Dispose()
    {
        _userManager.Reset();
        _tokensService.Reset();
        _roleManager.Reset();
    }

    private static BlogUser MakeUser(string email = "user@example.com")
    {
        return new BlogUser
        {
            UserName = email,
            Email = email,
            DisplayName = "User Name"
        };
    }

    private static RefreshToken MakeRefreshToken(Guid userId, bool used = false, bool invalidated = false,
        DateTimeOffset? expirationDate = null)
    {
        return new RefreshToken
        {
            Token = $"{Guid.NewGuid()}",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate ?? DateTimeOffset.UtcNow.AddDays(30),
            Used = used,
            Invalidated = invalidated,
            UserId = userId
        };
    }

    [Fact]
    public async Task Login_ReturnsFailure_WhenUserDoesNotExist()
    {
        _userManager.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync((BlogUser?)null);

        AuthenticationResult result = await _authService.Login("user@example.com", "password");

        result.Success.Should().BeFalse();
        result.User.Should().BeNull();
        result.Errors.Should().NotBeNullOrEmpty();
        _userManager.Verify(x => x.CheckPasswordAsync(It.IsAny<BlogUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsFailure_WhenPasswordIsInvalid()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);

        AuthenticationResult result = await _authService.Login(user.Email!, "wrong-password");

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
        _tokensService.Verify(x => x.GenerateAccessToken(It.IsAny<BlogUser>(), It.IsAny<IReadOnlyCollection<Claim>>()),
            Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsSuccess_WhenCredentialsAreValid()
    {
        BlogUser user = MakeUser();
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);
        _tokensService.Setup(x => x.GenerateAndSaveRefreshToken(user))
            .ReturnsAsync(refreshToken);

        AuthenticationResult result = await _authService.Login(user.Email!, "password");

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be(refreshToken.Token);
        result.User.Should().Be(user);
        result.Errors.Should().BeNull();
    }

    [Fact]
    public async Task Login_IncludesRoleClaims_InGeneratedAccessToken()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin", "Editor" });

        await _authService.Login(user.Email!, "password");

        _tokensService.Verify(x => x.GenerateAccessToken(user,
                It.Is<IReadOnlyCollection<Claim>>(claims =>
                    claims.Any(c => c.Type == "roles" && c.Value == "Admin") &&
                    claims.Any(c => c.Type == "roles" && c.Value == "Editor"))),
            Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsFailure_WhenUserAlreadyExists()
    {
        BlogUser existingUser = MakeUser();
        _userManager.Setup(x => x.FindByEmailAsync(existingUser.Email!)).ReturnsAsync(existingUser);

        AuthenticationResult result = await _authService.Register("newuser", existingUser.Email!, "password");

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
        _userManager.Verify(x => x.CreateAsync(It.IsAny<BlogUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_ReturnsFailure_WhenCreateFails()
    {
        _userManager.Setup(x => x.FindByEmailAsync("newuser@example.com")).ReturnsAsync((BlogUser?)null);
        IdentityResult failedResult =
            IdentityResult.Failed(new IdentityError { Description = "Password too weak." });
        _userManager.Setup(x => x.CreateAsync(It.IsAny<BlogUser>(), "weak")).ReturnsAsync(failedResult);

        AuthenticationResult result = await _authService.Register("newuser", "newuser@example.com", "weak");

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Password too weak.");
    }

    [Fact]
    public async Task Register_ReturnsSuccess_WhenCreateSucceeds()
    {
        _userManager.Setup(x => x.FindByEmailAsync("newuser@example.com")).ReturnsAsync((BlogUser?)null);
        _userManager.Setup(x => x.CreateAsync(
                It.Is<BlogUser>(u => u.UserName == "newuser@example.com" && u.Email == "newuser@example.com" &&
                                     u.DisplayName == "newuser"),
                "password"))
            .ReturnsAsync(IdentityResult.Success);
        RefreshToken refreshToken = MakeRefreshToken(Guid.NewGuid());
        _tokensService.Setup(x => x.GenerateAndSaveRefreshToken(It.Is<BlogUser>(u =>
                u.UserName == "newuser@example.com" && u.Email == "newuser@example.com" &&
                u.DisplayName == "newuser")))
            .ReturnsAsync(refreshToken);

        AuthenticationResult result = await _authService.Register("newuser", "newuser@example.com", "password");

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be(refreshToken.Token);
    }

    [Fact]
    public async Task Register_AssignRolesToUser_WhenRolesAreProvided()
    {
        const string displayName = "newuser";
        const string email = "newuser@example.com";
        _userManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((BlogUser?)null);
        _userManager.Setup(x => x.CreateAsync(
                It.Is<BlogUser>(u => u.UserName == email && u.Email == email &&
                                     u.DisplayName == displayName),
                "password"))
            .ReturnsAsync(IdentityResult.Success);

        List<string> userRoles = ["Admin"];
        AuthenticationResult result =
            await _authService.Register(displayName, email, "password", userRoles);


        result.Success.Should().BeTrue();
        _userManager.Verify(x => x.AddToRolesAsync(It.Is<BlogUser>(u =>
                    u.UserName == email && u.Email == email &&
                    u.DisplayName == displayName),
                userRoles),
            Times.Once);
    }

    [Fact]
    public async Task Register_DoesNotAssignRoles_WhenRolesAreNotProvided()
    {
        const string displayName = "newuser";
        const string email = "newuser@example.com";
        _userManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((BlogUser?)null);
        _userManager.Setup(x => x.CreateAsync(
                It.Is<BlogUser>(u => u.UserName == email && u.Email == email &&
                                     u.DisplayName == displayName),
                "password"))
            .ReturnsAsync(IdentityResult.Success);

        AuthenticationResult result =
            await _authService.Register(displayName, email, "password");


        result.Success.Should().BeTrue();
        _userManager.Verify(x => x.AddToRolesAsync(It.Is<BlogUser>(u =>
                    u.UserName == email && u.Email == email &&
                    u.DisplayName == displayName),
                It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenUserDoesNotExist()
    {
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((BlogUser?)null);
        RefreshToken refreshToken = MakeRefreshToken(Guid.NewGuid());

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        _tokensService.Verify(x => x.UseRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenTokenWasAlreadyUsed()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id, true);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        _tokensService.Verify(x => x.UseRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenTokenIsExpired()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken =
            MakeRefreshToken(user.Id, expirationDate: DateTimeOffset.UtcNow.AddMinutes(-1));

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        _tokensService.Verify(x => x.UseRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenTokenIsInvalidated()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id, invalidated: true);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        _tokensService.Verify(x => x.UseRefreshToken(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsSuccess_WhenTokenIsValid()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be(replacementToken.Token);
        _tokensService.Verify(x => x.UseRefreshToken(refreshToken, replacementToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsSuccess_WhenUsedTokenIsWithinGracePeriod()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .Callback<RefreshToken, RefreshToken>((t, r) =>
            {
                t.Used = true;
                t.UsedDate = _timeProvider.GetUtcNow();
                t.ReplacedByToken = r;
            });

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);
        result.Success.Should().BeTrue();

        _timeProvider.Advance(RefreshToken.UsedGracePeriod - TimeSpan.FromMilliseconds(100));
        result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenUsedTokenIsPastGracePeriod()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .Callback<RefreshToken, RefreshToken>((t, r) =>
            {
                t.Used = true;
                t.UsedDate = _timeProvider.GetUtcNow();
                t.ReplacedByToken = r;
            });

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);
        result.Success.Should().BeTrue();

        _timeProvider.Advance(RefreshToken.UsedGracePeriod + TimeSpan.FromMilliseconds(100));
        result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokens_ReturnsSuccess_WhenConcurrencyConflictHasReplacementWithinGracePeriod()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        RefreshToken winningReplacement = MakeRefreshToken(user.Id);
        RefreshToken refetchedToken = MakeRefreshToken(user.Id, true);
        refetchedToken.UsedDate = _timeProvider.GetUtcNow();
        refetchedToken.ReplacedByToken = winningReplacement;

        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        _tokensService.Setup(x => x.GetRefreshToken(refreshToken.Token, true)).ReturnsAsync(refetchedToken);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be(winningReplacement.Token);
        _tokensService.Verify(x => x.GenerateAccessToken(user, It.IsAny<IReadOnlyCollection<Claim>>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenConcurrencyConflictHasNoReplacement()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        RefreshToken refetchedToken = MakeRefreshToken(user.Id);

        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        _tokensService.Setup(x => x.GetRefreshToken(refreshToken.Token, true)).ReturnsAsync(refetchedToken);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenConcurrencyConflictReplacementIsPastGracePeriod()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);
        RefreshToken winningReplacement = MakeRefreshToken(user.Id);
        RefreshToken refetchedToken = MakeRefreshToken(user.Id, true);
        refetchedToken.UsedDate = _timeProvider.GetUtcNow();
        refetchedToken.ReplacedByToken = winningReplacement;

        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        _tokensService.Setup(x => x.GetRefreshToken(refreshToken.Token, true)).ReturnsAsync(refetchedToken);

        _timeProvider.Advance(RefreshToken.UsedGracePeriod + TimeSpan.FromMilliseconds(100));
        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokens_ReturnsFailure_WhenConcurrencyConflictRefetchReturnsNull()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id);
        RefreshToken replacementToken = MakeRefreshToken(user.Id);

        _tokensService.Setup(x => x.CreateRefreshToken(user)).Returns(replacementToken);
        _tokensService.Setup(x => x.UseRefreshToken(refreshToken, replacementToken))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        _tokensService.Setup(x => x.GetRefreshToken(refreshToken.Token, true)).ReturnsAsync((RefreshToken?)null);

        AuthenticationResult result = await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokens_Throws_WhenUsedTokenWithinGracePeriodHasNoReplacement()
    {
        BlogUser user = MakeUser();
        _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        RefreshToken refreshToken = MakeRefreshToken(user.Id, true);
        refreshToken.UsedDate = _timeProvider.GetUtcNow();
        refreshToken.ReplacedByToken = null;

        Func<Task> act = async () => await _authService.RefreshTokens(new ClaimsPrincipal(), refreshToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Logout_ShouldRevokeToken_WhenCalled()
    {
        RefreshToken refreshToken = MakeRefreshToken(Guid.NewGuid());

        await _authService.Logout(refreshToken);

        _tokensService.Verify(x => x.RevokeRefreshToken(refreshToken), Times.Once);
    }
}