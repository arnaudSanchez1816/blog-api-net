using System.Text.Json.Serialization;

namespace BlogApi.Contracts.V1.Requests;

[JsonConverter(typeof(JsonStringEnumConverter<PostSortOption>))]
public enum PostSortOption
{
    [JsonStringEnumMemberName("id")]
    IdAscending,

    [JsonStringEnumMemberName("-id")]
    IdDescending,

    [JsonStringEnumMemberName("publishedAt")]
    PublishedAtAscending,

    [JsonStringEnumMemberName("-publishedAt")]
    PublishedAtDescending
}