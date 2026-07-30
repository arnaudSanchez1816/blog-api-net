using System.Text.Json.Serialization;

namespace BlogApi.Contracts.V1.Responses;

public record GetTagsResponse
{
    [JsonPropertyName("results")]
    public required List<TagResponse> Tags { get; init; }

    public required PagedResponseMetadata Metadata { get; init; }
}