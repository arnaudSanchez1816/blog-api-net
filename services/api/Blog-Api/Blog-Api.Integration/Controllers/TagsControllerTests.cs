using System.Net;
using System.Net.Http.Json;
using System.Text;
using AwesomeAssertions;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Repositories.Tags;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class TagsControllerTests : IntegrationTestBase
{
    private const string InvalidSlug = "@--";

    private const string UnicodeSlug = "café-résumé";

    private static readonly string SlugAtMaxLength = BuildSlugOfLength(64);
    private static readonly string SlugOverMaxLength = BuildSlugOfLength(65);
    private static readonly string NameAtMaxLength = new string('a', 64);
    private static readonly string NameOverMaxLength = new string('a', 65);

    private ITagsRepository _tagsRepository = null!;

    private HttpClient HttpClient
    {
        get => Factory.HttpClient;
    }

    public TagsControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    private static string BuildSlugOfLength(int length)
    {
        // Alternates letters with hyphens (a-a-a-a-...) which satisfies SlugGenerator.Pattern
        // (^[a-z0-9]+(?:-[a-z0-9]+)*$) regardless of requested length.
        char[] chars = new char[length];
        for (int i = 0; i < length; i++) chars[i] = i % 2 == 0 ? 'a' : '-';

        if (chars[^1] == '-')
        {
            chars[^1] = 'a';
        }

        return new string(chars);
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

    [Fact]
    public async Task GetAllTags_ReturnsAllTags_WhenTagsExist()
    {
        Tag tag1 = new Tag { Name = "tag1", Slug = "tag-1-slug" };
        Tag tag2 = new Tag { Name = "tag2", Slug = "tag-2-slug" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/tags", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetTagsResponse? body =
            await response.Content.ReadFromJsonAsync<GetTagsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.Count.Should().Be(2);
        body.Metadata.PageNumber.Should().BeNull();
        body.Metadata.PageSize.Should().BeNull();
        body.Metadata.SortBy.Should().BeNull();
        body.Tags.Should().HaveCount(2);
        body.Tags.Should().Contain(t => t.Slug == tag1.Slug);
        body.Tags.Should().Contain(t => t.Slug == tag2.Slug);
    }

    [Fact]
    public async Task GetById_ReturnsTag_WhenTagExists()
    {
        Tag tag = new Tag { Name = "DotNet", Slug = "dotnet" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/tags/id/{tag.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(tag.Id);
        body.Name.Should().Be(tag.Name);
        body.Slug.Should().Be(tag.Slug);
    }

    [Fact]
    public async Task GetById_Returns404_WhenIdDoesNotExist()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1.0/tags/id/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenGivenValidNewTagData()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Name.Should().Be(request.Name);
        body.Slug.Should().Be(request.Slug);
        response.Headers.Location.Should().NotBeNull();

        HttpResponseMessage locationResponse =
            await HttpClient.GetAsync(response.Headers.Location, TestContext.Current.CancellationToken);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? locationBody =
            await locationResponse.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        locationBody.Should().NotBeNull();
        locationBody.Name.Should().Be(request.Name);
        locationBody.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenGivenAnInvalidSlug()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = InvalidSlug };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenNameIsTooLong()
    {
        CreateTagRequest request = new CreateTagRequest { Name = NameOverMaxLength, Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsTooLong()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = SlugOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsEmpty()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = "" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenNameIsEmpty()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "", Slug = "tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenRequestBodyIsEmpty()
    {
        using StringContent emptyJsonContent = new StringContent("{}", Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await HttpClient.PostAsync("api/v1.0/tags", emptyJsonContent,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns409_WhenSlugAlreadyExists()
    {
        Tag existingTag = new Tag { Name = "Existing", Slug = "existing-slug" };
        await _tagsRepository.AddTag(existingTag);
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = existingTag.Slug };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenSlugIsOneCharacter()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "A", Slug = "a" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be("a");
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenSlugIsExactlyAtMaxLength()
    {
        SlugAtMaxLength.Length.Should().Be(64);
        CreateTagRequest request = new CreateTagRequest { Name = "Boundary tag", Slug = SlugAtMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be(SlugAtMaxLength);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsOneCharacterOverMaxLength()
    {
        SlugOverMaxLength.Length.Should().Be(65);
        CreateTagRequest request = new CreateTagRequest { Name = "Boundary tag", Slug = SlugOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenNameIsExactlyAtMaxLength()
    {
        NameAtMaxLength.Length.Should().Be(64);
        CreateTagRequest request = new CreateTagRequest { Name = NameAtMaxLength, Slug = "boundary-name-tag" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Name.Should().Be(NameAtMaxLength);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugContainsNonAsciiCharacters()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = UnicodeSlug };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/tags", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugContainsNonAsciiCharacters()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = UnicodeSlug };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_ReturnsUpdatedTag_WhenGivenValidData()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "UpdatedTag", Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Name.Should().Be(request.Name);
        body.Slug.Should().Be(request.Slug);

        HttpResponseMessage followUpResponse = await HttpClient.GetAsync($"api/v1.0/tags/{request.Slug}",
            TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? followUpBody = await followUpResponse.Content.ReadFromJsonAsync<TagResponse>(
            TestContext.Current.CancellationToken);
        followUpBody.Should().NotBeNull();
        followUpBody.Name.Should().Be(request.Name);
        followUpBody.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenNameIsTooLong()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = NameOverMaxLength, Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugIsTooLong()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = SlugOverMaxLength };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugIsInvalid()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = InvalidSlug };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_LeavesNameUnchanged_WhenNameIsOmitted()
    {
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = null, Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse = await HttpClient.GetAsync($"api/v1.0/tags/{request.Slug}",
            TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? followUpBody = await followUpResponse.Content.ReadFromJsonAsync<TagResponse>(
            TestContext.Current.CancellationToken);
        followUpBody.Should().NotBeNull();
        followUpBody.Name.Should().Be(tag.Name);
        followUpBody.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task UpdateTag_LeavesSlugUnchanged_WhenSlugIsOmitted()
    {
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "UpdatedName", Slug = null };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse = await HttpClient.GetAsync($"api/v1.0/tags/{tag.Slug}",
            TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? followUpBody = await followUpResponse.Content.ReadFromJsonAsync<TagResponse>(
            TestContext.Current.CancellationToken);
        followUpBody.Should().NotBeNull();
        followUpBody.Name.Should().Be(request.Name);
        followUpBody.Slug.Should().Be(tag.Slug);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenRequestBodyIsEmptyObject()
    {
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            new UpdateTagRequest(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugParamIsInvalid()
    {
        UpdateTagRequest request = new UpdateTagRequest { Name = "tagName", Slug = "tag-slug" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{InvalidSlug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns404_WhenTagSlugNotFound()
    {
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = "new-slug" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync("api/v1.0/tags/not-found-slug", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTag_Returns409_WhenSlugAlreadyExists()
    {
        Tag existingTag = new Tag { Name = "Existing", Slug = "existing-slug" };
        Tag otherTag = new Tag { Name = "Other", Slug = "other-tag" };
        await _tagsRepository.AddTag(existingTag);
        await _tagsRepository.AddTag(otherTag);
        UpdateTagRequest request = new UpdateTagRequest { Slug = otherTag.Slug };

        HttpResponseMessage response =
            await HttpClient.PutAsJsonAsync($"api/v1.0/tags/{existingTag.Slug}", request,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteTag_ReturnsDeletedTag_WhenGivenValidSlug()
    {
        Tag tag = new Tag { Name = "Tag", Slug = "tag-to-delete" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"api/v1.0/tags/{tag.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be(tag.Slug);

        HttpResponseMessage followUpResponse =
            await HttpClient.GetAsync($"api/v1.0/tags/{tag.Slug}", TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTag_Returns400_WhenSlugParamIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"api/v1.0/tags/{InvalidSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTag_Returns404_WhenSlugNotFound()
    {
        HttpResponseMessage response = await HttpClient.DeleteAsync("api/v1.0/tags/slug-not-found",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}