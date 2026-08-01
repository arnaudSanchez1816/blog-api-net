using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Comments;
using FluentValidation;

namespace BlogApi.Validation.Validators.Posts;

public class CreatePostCommentRequestValidator : AbstractValidator<CreatePostCommentRequest>
{
    public CreatePostCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage(CommentsValidationConstants.BodyNotEmptyMessage)
            .MaximumLength(Comment.BodyMaxLength)
            .WithMessage(CommentsValidationConstants.BodyTooLongMessage);

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage(CommentsValidationConstants.UsernameEmptyMessage)
            .MaximumLength(Comment.UsernameMaxLength)
            .WithMessage(CommentsValidationConstants.UsernameTooLongMessage);
    }
}