using BlogApi.Domain;

namespace BlogApi.Validation.Validators.Posts;

public static class PostsValidationConstants
{
    public const int BodyMaxLength = 50000; // 8-10k words, we do it here and not in Post to keep type 'text' sql side.

    public const string TitleNotEmptyMessage = "Post title cannot be empty or blank.";

    public const string AtLeastOneFieldMessage =
        "At least one of Title, Body, Tags or IsPublished must be provided.";

    public const string TagsMustBeUniqueMessage =
        "Each tag must be unique.";

    public static readonly string MustBeValidTagSlugMessage =
        $"Tags must be represented by a valid slug of {Tag.TagSlugMaxLength} characters maximum.";

    public static readonly string TitleMaxLengthMessage =
        $"Post titles are limited to {Post.TitleMaxLength} characters maximum.";

    public static readonly string BodyMaxLengthMessage =
        $"Post body is limited to {BodyMaxLength} characters maximum.";
}