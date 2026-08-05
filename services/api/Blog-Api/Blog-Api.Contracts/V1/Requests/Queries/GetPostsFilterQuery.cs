namespace BlogApi.Contracts.V1.Requests.Queries;

public record GetPostsFilterQuery
{
    public const PostSortOption DefaultSortOption = PostSortOption.PublishedAtDescending;

    public string Q { get; init; } = string.Empty;

    // Dot net core does not bind from query to enum using a JsonConverter on the enum
    // To do that we created a custom IModelBinder that do just that for the PostSortOption enum
    public PostSortOption SortBy { get; init; } = DefaultSortOption;

    public IReadOnlyCollection<string>? Tags { get; init; }

    public bool IncludeUnpublished { get; init; } = false;

    public Guid? Author { get; init; }

    public void Deconstruct(out string q, out PostSortOption sortBy, out IReadOnlyCollection<string>? tags,
        out bool includeUnpublished, out Guid? authorId)
    {
        q = Q;
        sortBy = SortBy;
        tags = Tags;
        includeUnpublished = IncludeUnpublished;
        authorId = Author;
    }
}