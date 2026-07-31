using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Tags;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Tags;

public class CreateTagRequestValidatorTests
{
    private readonly CreateTagRequestValidator _validator = new CreateTagRequestValidator();

    [Fact]
    public void Validate_HasError_WhenNameIsEmpty()
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = string.Empty,
            Slug = "tag-slug"
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_HasError_WhenNameExceedsMaxLength()
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = new string('a', Tag.TagNameMaxLength + 1),
            Slug = "tag-slug"
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(TagsValidationConstants.NameTooLongMessage);
    }

    [Fact]
    public void Validate_HasError_WhenSlugIsEmpty()
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = "Tag name",
            Slug = string.Empty
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Fact]
    public void Validate_HasError_WhenSlugExceedsMaxLength()
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = "Tag name",
            Slug = new string('a', Tag.TagSlugMaxLength + 1)
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Theory]
    [InlineData("àbcdé")]
    [InlineData("Not A Slug")]
    [InlineData("-leading-dash")]
    public void Validate_HasError_WhenSlugIsInvalid(string slug)
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = "Tag name",
            Slug = slug
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Fact]
    public void Validate_HasNoError_WhenAllFieldsAreValid()
    {
        CreateTagRequest request = new CreateTagRequest
        {
            Name = "Tag name",
            Slug = "tag-slug"
        };

        TestValidationResult<CreateTagRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
