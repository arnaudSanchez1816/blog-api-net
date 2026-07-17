using System.Text.Json.Serialization;
using BlogApi.Contracts.V1.Requests;

namespace BlogApi.Contracts.V1.Responses;

public record GetTagsResponse
{
    [JsonPropertyName("results")]
    public required List<TagResponse> Tags { get; init; }

    public required PaginationQueryMetadata Metadata { get; init; }
}