using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using FluentValidation;

namespace BlogApi.Validation.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Post title cannot be empty or blank.")
            .MaximumLength(Post.TitleMaxLength)
            .WithMessage($"Post title are limited to {Post.TitleMaxLength} characters maximum.");
    }
}