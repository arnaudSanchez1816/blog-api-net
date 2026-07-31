using BlogApi.Domain;

namespace BlogApi.Validation.Validators.Tags;

public static class TagsValidationConstants
{
    public static readonly string NameTooLongMessage = $"Tag name must be {Tag.TagNameMaxLength} characters maximum.";

    public static readonly string InvalidSlugMessage =
        $"Tags must be represented by a valid slug of {Tag.TagSlugMaxLength} characters maximum.";
}