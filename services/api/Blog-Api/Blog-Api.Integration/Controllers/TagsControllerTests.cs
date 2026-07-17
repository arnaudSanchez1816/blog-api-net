using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class TagsControllerTests : IntegrationTestBase
{
    private ITagsRepository _tagsRepository = null!;

    private HttpClient HttpClient
    {
        get { return Factory.HttpClient; }
    }

    public TagsControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override Task OnInitializeAsync()
    {
        _tagsRepository = GetRequiredService<ITagsRepository>();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetBySlug_ReturnsTag_WhenTagExists()
    {
        Tag tag = new Tag { Name = "DotNet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/tags/{tag.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be(tag.Slug);
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenTagDoesNotExists()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/tags/dotnet", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/tags/@--", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}