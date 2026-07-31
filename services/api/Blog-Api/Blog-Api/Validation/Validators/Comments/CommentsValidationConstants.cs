using BlogApi.Domain;

namespace BlogApi.Validation.Validators.Comments;

public static class CommentsValidationConstants
{
    public const string AtLeastOneFieldMessage = "At least one of Body or Username must be specified.";

    public const string BodyNotEmptyMessage = "Comment body must not be empty.";

    public const string UsernameEmptyMessage = "Comment username must not be empty.";

    public static readonly string BodyTooLongMessage =
        $"Comment body must be {Comment.BodyMaxLength} characters maximum.";

    public static readonly string UsernameTooLongMessage =
        $"Comment username must be {Comment.UsernameMaxLength} characters maximum.";
}