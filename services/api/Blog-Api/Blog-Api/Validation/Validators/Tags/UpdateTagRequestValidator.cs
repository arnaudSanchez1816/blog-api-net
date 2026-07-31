using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators.Tags;

public class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Slug is not null)
            .WithMessage(TagsValidationConstants.AtLeastOneFieldMessage)
            .WithName(nameof(UpdateTagRequest));

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Tag.TagNameMaxLength)
            .WithMessage(TagsValidationConstants.NameTooLongMessage)
            .When(x => x.Name is not null);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage(TagsValidationConstants.InvalidSlugMessage)
            .MaximumLength(Tag.TagSlugMaxLength)
            .WithMessage(TagsValidationConstants.InvalidSlugMessage)
            .Matches(SlugGenerator.Pattern)
            .WithMessage(TagsValidationConstants.InvalidSlugMessage)
            .When(x => x.Slug is not null);
    }
}