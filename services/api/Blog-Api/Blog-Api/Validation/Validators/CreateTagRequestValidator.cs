using BlogApi.Contracts.V1.Requests;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(64).Matches(SlugGenerator.Pattern);
    }
}