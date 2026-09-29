using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BlogApi.Authentication;

/// <summary>
/// This returns a 401 when a Jwt is provided but is invalid
/// </summary>
public class InvalidBearerTokenMiddleware
{
    private readonly RequestDelegate _next;

    public InvalidBearerTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // We could also have plugged this behavior in authenticationBuilder.AddJwtBearer() in AuthenticationInstaller
    // instead of a middleware
    public async Task InvokeAsync(HttpContext context)
    {
        // Technically this calls authenticate a second time but the previous result was cached so this is fine
        AuthenticateResult authenticateResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (authenticateResult.Failure != null)
        {
            // Jwt provided and is invalid, returns a 401
            await context.ChallengeAsync(JwtBearerDefaults.AuthenticationScheme);
            return;
        }

        await _next(context);
    }
}