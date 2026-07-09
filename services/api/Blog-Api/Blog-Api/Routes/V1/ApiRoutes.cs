namespace BlogApi.Routes.V1;

public static class ApiRoutes
{
    private const string Root = "api";
    private const string Base = $"{Root}/v{{version:apiVersion}}";

    public static class Posts
    {
        public const string Base = $"{ApiRoutes.Base}/posts";

        public const string GetAll = "";
        public const string GetBySlug = "{slug}";
    }
}