using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Posts;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Posts;

public class CreatePostRequestValidatorTests
{
    private readonly CreatePostRequestValidator _validator = new CreatePostRequestValidator();

    [Fact]
    public void Validate_HasNoError_WhenTitleIsValid()
    {
        CreatePostRequest request = new CreatePostRequest
        {
            Title = "A valid title"
        };

        TestValidationResult<CreatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // We need to check that non nullable fields are not null.
    // Properties set to "required" and missing in the request body will automatically send a 400 by dot net core
    // before reaching the validator or controller
    [Fact]
    public void Validate_HasError_WhenTitleIsNull()
    {
        CreatePostRequest request = new CreatePostRequest
        {
            Title = null!
        };

        TestValidationResult<CreatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(PostsValidationConstants.TitleNotEmptyMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTitleIsEmpty()
    {
        CreatePostRequest request = new CreatePostRequest
        {
            Title = ""
        };

        TestValidationResult<CreatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(PostsValidationConstants.TitleNotEmptyMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTitleIsOverMaxLength()
    {
        CreatePostRequest request = new CreatePostRequest
        {
            Title = new string('a', Post.TitleMaxLength + 1)
        };

        TestValidationResult<CreatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(PostsValidationConstants.TitleMaxLengthMessage);
    }
}