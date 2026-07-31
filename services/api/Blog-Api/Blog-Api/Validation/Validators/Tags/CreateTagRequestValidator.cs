using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators.Tags;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Tag.TagNameMaxLength)
            .WithMessage(TagsValidationConstants.NameTooLongMessage);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage(TagsValidationConstants.InvalidSlugMessage)
            .MaximumLength(Tag.TagSlugMaxLength)
            .WithMessage(TagsValidationConstants.InvalidSlugMessage)
            .Matches(SlugGenerator.Pattern)
            .WithMessage(TagsValidationConstants.InvalidSlugMessage);
    }
}