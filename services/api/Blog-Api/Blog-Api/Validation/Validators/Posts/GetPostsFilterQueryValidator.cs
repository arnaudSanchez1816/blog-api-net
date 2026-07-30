using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Utils;
using FluentValidation;

namespace BlogApi.Validation.Validators.Posts;

public class GetPostsFilterQueryValidator : AbstractValidator<GetPostsFilterQuery>
{
    public GetPostsFilterQueryValidator()
    {
        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(64)
            .WithMessage(PostsValidationConstants.MustBeValidTagSlugMessage)
            .Matches(SlugGenerator.Pattern)
            .WithMessage(PostsValidationConstants.MustBeValidTagSlugMessage)
            .When(x => x.Tags is not null);
    }
}
