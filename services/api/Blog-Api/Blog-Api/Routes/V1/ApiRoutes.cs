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
        public const string Create = "";
        public const string UpdateBySlug = "{slug}";
        public const string DeleteBySlug = "{slug}";

        public const string GetCommentsBySlug = "{slug}/comments";
        public const string CreateCommentBySlug = "{slug}/comments";
    }

    public static class Tags
    {
        public const string Base = $"{ApiRoutes.Base}/tags";

        public const string GetBySlug = "{slug}";
        public const string GetById = "id/{id}";
        public const string GetAll = "";
        public const string Create = "";
        public const string DeleteBySlug = "{slug}";

        // tagSlug instead of slug to avoid ValidationProblemDetails errors map conflict between route param and body param
        public const string UpdateBySlug = "{tagSlug}";
    }

    public static class Comments
    {
        public const string Base = $"{ApiRoutes.Base}/comments";

        public const string GetById = "{id}";
        public const string UpdateById = "{id}";
        public const string DeleteById = "{id}";
    }

    public static class Auth
    {
        public const string Base = $"{ApiRoutes.Base}/auth";

        public const string Login = "login";
        public const string Logout = "logout";
        public const string GetAccessToken = "token";
    }

    public static class Users
    {
        public const string Base = $"{ApiRoutes.Base}/users";

        public const string GetCurrentUser = "me";
        public const string GetCurrentUserPosts = "me/posts";
    }
}