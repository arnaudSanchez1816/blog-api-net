using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using FluentValidation;

namespace BlogApi.Validation.Validators.Posts;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(PostsValidationConstants.TitleNotEmptyMessage)
            .MaximumLength(Post.TitleMaxLength)
            .WithMessage(PostsValidationConstants.TitleMaxLengthMessage);
    }
}