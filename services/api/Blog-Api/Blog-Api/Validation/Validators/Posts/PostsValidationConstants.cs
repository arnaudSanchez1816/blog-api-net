using BlogApi.Domain;

namespace BlogApi.Validation.Validators.Posts;

public static class PostsValidationConstants
{
    public const string TitleNotEmptyMessage = "Post title cannot be empty or blank.";

    public const string AtLeastOneFieldMessage =
        "At least one of Title, Body or Tags must be provided.";

    public const string TagsMustBeUniqueMessage =
        "Each tag must be unique.";

    public static readonly string MustBeValidTagSlugMessage =
        $"Tags must be represented by a valid slug of {Tag.TagSlugMaxLength} characters maximum.";

    public static readonly string TitleMaxLengthMessage =
        $"Post title are limited to {Post.TitleMaxLength} characters maximum.";
}