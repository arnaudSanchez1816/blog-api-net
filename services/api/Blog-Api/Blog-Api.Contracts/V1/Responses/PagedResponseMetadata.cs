using System.Text.Json.Serialization;

namespace BlogApi.Contracts.V1.Responses;

public record PagedResponseMetadata
{
    public long Count { get; init; }

    /// <summary>
    /// Current page
    /// </summary>
    /// <example>1</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageNumber { get; init; }

    /// <summary>
    /// Size of the page
    /// </summary>
    /// <example>20</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageSize { get; init; }

    /// <summary>
    /// Sort applied to request
    /// </summary>
    /// <example>id</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SortBy { get; init; }
}