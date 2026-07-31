using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using FluentValidation;

namespace BlogApi.Validation.Validators.Comments;

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Username is not null || x.Body is not null)
            .WithMessage(CommentsValidationConstants.AtLeastOneFieldMessage)
            .WithName(nameof(UpdateCommentRequest));

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage(CommentsValidationConstants.BodyNotEmptyMessage)
            .MaximumLength(Comment.BodyMaxLength)
            .WithMessage(CommentsValidationConstants.BodyTooLongMessage)
            .When(x => x.Body is not null);

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage(CommentsValidationConstants.UsernameEmptyMessage)
            .MaximumLength(Comment.UsernameMaxLength)
            .WithMessage(CommentsValidationConstants.UsernameTooLongMessage)
            .When(x => x.Username is not null);
    }
}