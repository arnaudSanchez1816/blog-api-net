using BlogApi.Domain;

namespace BlogApi.Services.Auth;

public record AuthenticationResult
{
    public required bool Success { get; init; }

    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public IReadOnlyCollection<string>? Errors { get; init; }

    public BlogUser? User { get; init; }
}