using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Comments;
using BlogApi.Validation.Validators.Posts;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Posts;

public class CreatePostCommentRequestValidatorTests
{
    private readonly CreatePostCommentRequestValidator _validator = new CreatePostCommentRequestValidator();

    [Fact]
    public void Validate_Success_WhenRequestIsValid()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = "comment body",
            Username = "username"
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Fail_WhenUsernameIsNull()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = "comment body",
            Username = null!
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(CommentsValidationConstants.UsernameEmptyMessage);
    }

    [Fact]
    public void Validate_Fail_WhenBodyIsNull()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = null!,
            Username = "username"
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage(CommentsValidationConstants.BodyNotEmptyMessage);
    }

    [Fact]
    public void Validate_Fail_WhenUsernameIsEmpty()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = "comment body",
            Username = string.Empty
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(CommentsValidationConstants.UsernameEmptyMessage);
    }

    [Fact]
    public void Validate_Fail_WhenBodyIsEmpty()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = string.Empty,
            Username = "username"
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage(CommentsValidationConstants.BodyNotEmptyMessage);
    }

    [Fact]
    public void Validate_Fail_WhenBodyIsTooLong()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = new string('a', Comment.BodyMaxLength + 1),
            Username = "username"
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage(CommentsValidationConstants.BodyTooLongMessage);
    }

    [Fact]
    public void Validate_Fail_WhenUsernameIsTooLong()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest
        {
            Body = "body",
            Username = new string('a', Comment.UsernameMaxLength + 1)
        };

        TestValidationResult<CreatePostCommentRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage(CommentsValidationConstants.UsernameTooLongMessage);
    }
}