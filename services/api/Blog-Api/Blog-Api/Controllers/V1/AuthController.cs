using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Authentication;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Mapping;
using BlogApi.Routes.V1;
using BlogApi.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Auth.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Log in a user
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <returns>A valid access token</returns>
    [HttpPost(ApiRoutes.Auth.Login)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        AuthenticationResult result = await _authService.Login(request.Email, request.Password, ct);

        if (!result.Success)
        {
            // Return Problem instead of just BadRequest() so we can pass the error message.
            // BadRequest() calls with a parameter are not formatted as ProblemDetails by the ProblemDetailsMiddleware
            return Problem(string.Join("\n", result.Errors!),
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid login");
        }

        Response.Cookies.Append(RefreshTokenAuthDefaults.RefreshTokenCookie,
            result.RefreshToken!,
            GetRefreshTokenCookieOptions(RefreshTokenAuthDefaults.RefreshTokenCookieMaxAge));
        return Ok(new LoginResponse
        {
            AccessToken = result.AccessToken!,
            User = result.User!.ToUserResponse()
        });
    }

    /// <summary>
    /// Log out a user, clears the refresh token cookie
    /// </summary>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Auth.Logout)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Try to authenticate with the Refresh token
        AuthenticateResult authenticateResult =
            await HttpContext.AuthenticateAsync(RefreshTokenAuthDefaults.RefreshTokenScheme);

        if (authenticateResult.Succeeded && HttpContext.Items.TryGetValue(
                RefreshTokenAuthDefaults.RefreshTokenHttpContextItem,
                out object? tokenObject) && tokenObject is RefreshToken refreshToken)
        {
            // We could authenticate with the refresh token, revoke it.
            await _authService.Logout(refreshToken, ct);
        }

        CookieOptions refreshTokenCookieOptions = GetRefreshTokenCookieOptions();
        Response.Cookies.Delete(RefreshTokenAuthDefaults.RefreshTokenCookie, refreshTokenCookieOptions);

        return Ok();
    }

    /// <summary>
    /// Generate a new access token for the authenticated user
    /// </summary>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="401">Refresh token is invalid</response>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [HttpGet(ApiRoutes.Auth.GetAccessToken)]
    [Authorize(AuthenticationSchemes = RefreshTokenAuthDefaults.RefreshTokenScheme)]
    public async Task<ActionResult<GetAccessTokenResponse>> GetAccessToken(CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(RefreshTokenAuthDefaults.RefreshTokenHttpContextItem,
                out object? tokenObject) || tokenObject is not RefreshToken refreshToken)
        {
            throw new InvalidOperationException($"Expected a {nameof(RefreshToken)} in HttpContext.Items.");
        }

        AuthenticationResult result = await _authService.RefreshTokens(User, refreshToken, ct);
        if (!result.Success)
        {
            return Problem(string.Join("\n", result.Errors!),
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials");
        }

        // Update the refresh token cookie
        Response.Cookies.Append(RefreshTokenAuthDefaults.RefreshTokenCookie,
            result.RefreshToken!,
            GetRefreshTokenCookieOptions(RefreshTokenAuthDefaults.RefreshTokenCookieMaxAge));

        return Ok(new GetAccessTokenResponse
        {
            AccessToken = result.AccessToken!
        });
    }

    private CookieOptions GetRefreshTokenCookieOptions(TimeSpan? maxAge = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            MaxAge = maxAge,
            SameSite = SameSiteMode.Strict,
            Path = Url.Action(nameof(GetAccessToken))
        };
    }
}