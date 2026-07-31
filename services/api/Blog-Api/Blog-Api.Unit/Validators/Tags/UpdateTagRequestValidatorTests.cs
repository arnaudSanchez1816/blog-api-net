using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Validation.Validators.Tags;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Tags;

public class UpdateTagRequestValidatorTests
{
    private readonly UpdateTagRequestValidator _validator = new UpdateTagRequestValidator();

    [Fact]
    public void Validate_Success_WhenOnlySlugIsProvided()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Slug = "tag-slug"
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Success_WhenOnlyNameIsProvided()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = "Tag name"
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }


    [Fact]
    public void Validate_HasError_WhenNeitherNameOrSlugIsProvided()
    {
        UpdateTagRequest request = new UpdateTagRequest();

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(nameof(UpdateTagRequest))
            .WithErrorMessage(TagsValidationConstants.AtLeastOneFieldMessage);
    }

    [Fact]
    public void Validate_HasError_WhenNameIsEmpty()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = string.Empty,
            Slug = "tag-slug"
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_HasError_WhenNameExceedsMaxLength()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = new string('a', Tag.TagNameMaxLength + 1),
            Slug = "tag-slug"
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(TagsValidationConstants.NameTooLongMessage);
    }

    [Fact]
    public void Validate_HasError_WhenSlugIsEmpty()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = "Tag name",
            Slug = string.Empty
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Fact]
    public void Validate_HasError_WhenSlugExceedsMaxLength()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = "Tag name",
            Slug = new string('a', Tag.TagSlugMaxLength + 1)
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Theory]
    [InlineData("àbcdé")]
    [InlineData("Not A Slug")]
    [InlineData("-leading-dash")]
    public void Validate_HasError_WhenSlugIsInvalid(string slug)
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = "Tag name",
            Slug = slug
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage(TagsValidationConstants.InvalidSlugMessage);
    }

    [Fact]
    public void Validate_HasNoError_WhenAllFieldsAreValid()
    {
        UpdateTagRequest request = new UpdateTagRequest
        {
            Name = "Tag name",
            Slug = "tag-slug"
        };

        TestValidationResult<UpdateTagRequest> result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}