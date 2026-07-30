using System.Text.Json;
using System.Text.Json.Serialization;
using BlogApi.Contracts.V1.Requests.Queries;

namespace BlogApi.Contracts.V1.Responses;

public record GetPostsResponse
{
    [JsonPropertyName("results")]
    public required IReadOnlyCollection<PostResponse> Posts { get; init; }

    public required PagedResponseMetadata Metadata { get; init; }

    private GetPostsResponse()
    {
    }

    public static GetPostsResponse Create(IReadOnlyCollection<PostResponse> posts, int totalCount,
        GetPostsFilterQuery filter, PaginationQuery pagination)
    {
        GetPostsResponse response = new GetPostsResponse
        {
            Posts = posts,
            Metadata = new PagedResponseMetadata
            {
                Count = totalCount,
                PageNumber = pagination.PageNumber >= 1 ? pagination.PageNumber : null,
                PageSize = pagination.PageSize >= 1 ? pagination.PageSize : null,
                SortBy = filter.SortBy != GetPostsFilterQuery.DefaultSortOption
                    ? JsonSerializer.Serialize(filter.SortBy).Trim('"')
                    : null
            }
        };

        return response;
    }
}