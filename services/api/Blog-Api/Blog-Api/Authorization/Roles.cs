namespace BlogApi.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string User = "User";

    public static class Permissions
    {
        public static readonly IReadOnlyList<string> AdminPermissions =
        [
            // Posts
            Authorization.Permissions.Posts.Read,
            Authorization.Permissions.Posts.ReadUnpublished,
            Authorization.Permissions.Posts.Create,
            Authorization.Permissions.Posts.Update,
            Authorization.Permissions.Posts.Delete,
            // Tags
            Authorization.Permissions.Tags.Read,
            Authorization.Permissions.Tags.Create,
            Authorization.Permissions.Tags.Update,
            Authorization.Permissions.Tags.Delete,
            // Comments
            Authorization.Permissions.Comments.Read,
            Authorization.Permissions.Comments.Create,
            Authorization.Permissions.Comments.Update,
            Authorization.Permissions.Comments.Delete
        ];

        public static readonly IReadOnlyList<string> ModeratorPermissions =
        [
            // Posts
            Authorization.Permissions.Posts.Read,
            // Tags
            Authorization.Permissions.Tags.Read,
            // Comments
            Authorization.Permissions.Comments.Read,
            Authorization.Permissions.Comments.Create,
            Authorization.Permissions.Comments.Update,
            Authorization.Permissions.Comments.Delete
        ];

        public static readonly IReadOnlyList<string> RegisteredUserPermissions =
        [
            // Posts
            Authorization.Permissions.Posts.Read,
            // Tags
            Authorization.Permissions.Tags.Read,
            // Comments
            Authorization.Permissions.Comments.Read,
            Authorization.Permissions.Comments.Create
        ];

        public static readonly IReadOnlyList<string> AnonymousPermissions =
        [
            // Posts
            Authorization.Permissions.Posts.Read,
            // Tags
            Authorization.Permissions.Tags.Read,
            // Comments
            Authorization.Permissions.Comments.Read,
            Authorization.Permissions.Comments.Create
        ];
    }
}