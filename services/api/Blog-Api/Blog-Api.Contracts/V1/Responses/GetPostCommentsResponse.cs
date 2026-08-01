using System.Text.Json.Serialization;

namespace BlogApi.Contracts.V1.Responses;

public record GetPostCommentsResponse
{
    [JsonPropertyName("results")]
    public required IReadOnlyCollection<CommentResponse> Comments { get; init; }

    public required PagedResponseMetadata Metadata { get; init; }
}