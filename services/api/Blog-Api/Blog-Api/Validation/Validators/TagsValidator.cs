using BlogApi.Domain;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators;

public class TagsValidator : AbstractValidator<Tag>
{
    public TagsValidator()
    {
        RuleFor(x => x.Name).MaximumLength(64).NotEmpty();
        RuleFor(x => x.Slug).MaximumLength(64).NotEmpty().Matches(SlugGenerator.Pattern);
    }
}