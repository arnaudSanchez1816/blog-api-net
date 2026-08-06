namespace BlogApi.Authorization;

public static class Permissions
{
    public static string ToPermissionPolicy(string permission)
    {
        return $"{PermissionPolicyProvider.PermissionPolicyPrefix}{permission}";
    }

    public static class Tags
    {
        public const string Read = "tags.read";
        public const string Create = "tags.create";
        public const string Update = "tags.update";
        public const string Delete = "tags.delete";
    }

    public static class Posts
    {
        public const string Read = "posts.read";
        public const string ReadUnpublished = "posts.read.unpublished";
        public const string Create = "posts.create";
        public const string Update = "posts.update";
        public const string Delete = "posts.delete";
    }

    public static class Comments
    {
        public const string Read = "comments.read";
        public const string Create = "comments.create";
        public const string Update = "comments.update";
        public const string Delete = "comments.delete";
    }
}