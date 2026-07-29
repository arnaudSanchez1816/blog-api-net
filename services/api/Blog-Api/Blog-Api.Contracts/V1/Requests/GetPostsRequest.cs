namespace BlogApi.Contracts.V1.Requests;

public record GetPostsRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    public string Q { get; init; } = string.Empty;

    public int Page
    {
        get;
        init => field = Math.Max(0, value);
    }

    public int PageSize
    {
        get;
        init => field = Math.Clamp(value, 1, MaxPageSize);
    } = DefaultPageSize;

    // Dot net core does not bind from query to enum using a JsonConverter on the enum
    // To do that we created a custom IModelBinder that do just that for the PostSortOption enum
    public PostSortOption SortBy { get; init; } = PostSortOption.PublishedAtDescending;

    public IReadOnlyCollection<string>? Tags { get; init; }

    public bool IncludeUnpublished { get; init; } = false;
}