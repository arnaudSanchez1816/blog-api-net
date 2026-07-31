using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Validation.Validators.Posts;
using FluentValidation.TestHelper;

namespace BlogApi.Unit.Validators.Posts;

public class GetPostsFilterQueryValidatorTests
{
    private readonly GetPostsFilterQueryValidator _validator = new GetPostsFilterQueryValidator();


    [Fact]
    public void Validate_HasNoError_WhenTagIsValid()
    {
        GetPostsFilterQuery query = new GetPostsFilterQuery
        {
            Tags = new List<string>
            {
                "tag-slug",
                "other-slug"
            }
        };

        TestValidationResult<GetPostsFilterQuery> result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("àbcdé")]
    public void Validate_HasError_WhenTagIsInvalid(string tagSlug)
    {
        GetPostsFilterQuery query = new GetPostsFilterQuery
        {
            Tags = new List<string>
            {
                tagSlug
            }
        };

        TestValidationResult<GetPostsFilterQuery> result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Tags)
            .WithErrorMessage(PostsValidationConstants.MustBeValidTagSlugMessage);
    }
}