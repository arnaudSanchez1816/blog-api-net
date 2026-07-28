using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators.Posts;

public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        // One of not null
        RuleFor(x => x)
            .Must(x => x.Title is not null || x.Body is not null || x.Tags is not null)
            .WithMessage(PostsValidationConstants.AtLeastOneFieldMessage)
            .WithName(nameof(UpdatePostRequest));

        // Title
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(PostsValidationConstants.TitleNotEmptyMessage)
            .MaximumLength(Post.TitleMaxLength)
            .WithMessage(PostsValidationConstants.TitleMaxLengthMessage)
            .When(x => x.Title is not null);

        // Tags
        RuleFor(x => x.Tags)
            .Must(tags => tags!.Count == tags!.Distinct().Count())
            .WithMessage(PostsValidationConstants.TagsMustBeUniqueMessage).When(x => x.Tags is not null);
        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(64)
            .WithMessage(PostsValidationConstants.MustBeValidTagSlugMessage)
            .Matches(SlugGenerator.Pattern)
            .WithMessage(PostsValidationConstants.MustBeValidTagSlugMessage)
            .When(x => x.Tags is not null);
    }
}