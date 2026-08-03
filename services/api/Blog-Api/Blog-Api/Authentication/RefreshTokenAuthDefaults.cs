namespace BlogApi.Authentication;

public static class RefreshTokenAuthDefaults
{
    public const string RefreshTokenCookie = "__Http-REFRESHTOKEN";
    public const string RefreshTokenScheme = "RefreshToken";
    public const string RefreshTokenHttpContextItem = "RefreshToken";
    public static readonly TimeSpan RefreshTokenCookieMaxAge = TimeSpan.FromDays(30);
}