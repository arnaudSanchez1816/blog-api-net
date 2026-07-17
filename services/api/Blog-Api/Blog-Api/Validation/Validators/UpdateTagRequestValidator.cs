using BlogApi.Contracts.V1.Requests;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators;

public class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(64);
        RuleFor(x => x.Slug).MaximumLength(64).Matches(SlugGenerator.Pattern);
    }
}