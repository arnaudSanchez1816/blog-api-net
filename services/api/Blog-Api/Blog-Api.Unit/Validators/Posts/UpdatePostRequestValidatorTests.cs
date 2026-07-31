using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Posts;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Posts;

public class UpdatePostRequestValidatorTests
{
    private static readonly string SlugOverMaxLength = new string('a', Tag.TagSlugMaxLength + 1);
    private readonly UpdatePostRequestValidator _validator = new UpdatePostRequestValidator();

    [Fact]
    public void Validate_HasError_WhenAllFieldsAreNull()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(nameof(UpdatePostRequest))
            .WithErrorMessage(PostsValidationConstants.AtLeastOneFieldMessage);
    }

    [Fact]
    public void Validate_HasNoError_WhenOnlyTagsIsSet()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = ["tag-a"]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_HasNoError_WhenMultipleTagsAreSet()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = ["tag-a", "tag-b"]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("àbcdé")]
    public void Validate_HasError_WhenTagIsInvalid(string tagSlug)
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = [tagSlug]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Tags)
            .WithErrorMessage(PostsValidationConstants.MustBeValidTagSlugMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTagIsOverMaxLength()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = [SlugOverMaxLength]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Tags)
            .WithErrorMessage(PostsValidationConstants.MustBeValidTagSlugMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTagsAreDuplicate()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = null,
            Tags = ["tag-1", "tag-1"]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Tags)
            .WithErrorMessage(PostsValidationConstants.TagsMustBeUniqueMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTitleIsEmpty()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = string.Empty,
            Body = null,
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(PostsValidationConstants.TitleNotEmptyMessage);
    }

    [Fact]
    public void Validate_HasError_WhenTitleExceedsMaxLength()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = new string('a', Post.TitleMaxLength + 1),
            Body = null,
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage(PostsValidationConstants.TitleMaxLengthMessage);
    }

    [Fact]
    public void Validate_HasError_WhenBodyExceedsMaxLength()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = new string('a', PostsValidationConstants.BodyMaxLength + 1),
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Body)
            .WithErrorMessage(PostsValidationConstants.BodyMaxLengthMessage);
    }

    [Fact]
    public void Validate_HasNoError_WhenBodyIsAtMaxLength()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = new string('a', PostsValidationConstants.BodyMaxLength),
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void Validate_HasNoError_WhenTitleIsNullButAnotherFieldIsSet()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = null,
            Body = "Some body",
            Tags = null
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_HasNoError_WhenAllFieldsAreValid()
    {
        UpdatePostRequest request = new UpdatePostRequest
        {
            Title = "A valid title",
            Body = "A valid body",
            Tags = ["tag-a", "tag-b"]
        };

        TestValidationResult<UpdatePostRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}