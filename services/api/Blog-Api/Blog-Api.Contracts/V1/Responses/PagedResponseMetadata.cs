using System.Text.Json.Serialization;

namespace BlogApi.Contracts.V1.Responses;

public record PagedResponseMetadata
{
    public long Count { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageNumber { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageSize { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SortBy { get; init; }
}