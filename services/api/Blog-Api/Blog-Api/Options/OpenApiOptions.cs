namespace BlogApi.Options;

public class OpenApiOptions
{
    public const string ConfigurationSection = "OpenApiOptions";

    public string JsonRoute { get; set; } = "/openapi/{documentName}.json";
    public string UiEndpoint { get; set; } = "/scalar";
    public string Title { get; set; } = "Blog-Api";
    public bool Enable { get; init; } = false;
}