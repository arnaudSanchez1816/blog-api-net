using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using BlogApi.Authorization;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Integration.Extensions;
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

    #region GetAllTags

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
    public async Task GetAllTags_Returns403_WhenNoReadPermission()
    {
        (BlogUser _, string bearerToken) = await RegisterAuthenticatedUser();

        Tag tag1 = new Tag { Name = "tag1", Slug = "tag-1-slug" };
        Tag tag2 = new Tag { Name = "tag2", Slug = "tag-2-slug" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/tags", bearerToken, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetBySlug

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
    public async Task GetBySlug_Returns403_WhenNoReadPermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync("api/v1.0/tags/test-slug",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetById

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
    public async Task GetById_Returns403_WhenNoReadPermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUser();

        HttpResponseMessage response =
            await HttpClient.GetWithBearerAsync($"api/v1.0/tags/id/{Guid.NewGuid()}",
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region CreateTag

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenGivenValidNewTagData()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

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
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = InvalidSlug };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenNameIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = NameOverMaxLength, Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = SlugOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsEmpty()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = "" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenNameIsEmpty()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "", Slug = "tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenRequestBodyIsEmpty()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                new { },
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns409_WhenSlugAlreadyExists()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        Tag existingTag = new Tag { Name = "Existing", Slug = "existing-slug" };
        await _tagsRepository.AddTag(existingTag);
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = existingTag.Slug };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenSlugIsOneCharacter()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "A", Slug = "a" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be("a");
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenSlugIsExactlyAtMaxLength()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        SlugAtMaxLength.Length.Should().Be(64);
        CreateTagRequest request = new CreateTagRequest { Name = "Boundary tag", Slug = SlugAtMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Slug.Should().Be(SlugAtMaxLength);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugIsOneCharacterOverMaxLength()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        SlugOverMaxLength.Length.Should().Be(65);
        CreateTagRequest request = new CreateTagRequest { Name = "Boundary tag", Slug = SlugOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_ReturnsCreatedTag_WhenNameIsExactlyAtMaxLength()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        NameAtMaxLength.Length.Should().Be(64);
        CreateTagRequest request = new CreateTagRequest { Name = NameAtMaxLength, Slug = "boundary-name-tag" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        TagResponse? body =
            await response.Content.ReadFromJsonAsync<TagResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Name.Should().Be(NameAtMaxLength);
    }

    [Fact]
    public async Task CreateTag_Returns400_WhenSlugContainsNonAsciiCharacters()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Create]);
        CreateTagRequest request = new CreateTagRequest { Name = "New tag", Slug = UnicodeSlug };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Returns401_WhenNoBearerTokenIsProvided()
    {
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTag_Returns403_WhenUserHasNoCreatePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Read]);
        CreateTagRequest request = new CreateTagRequest { Name = "NewTag", Slug = "new-tag-slug" };

        HttpResponseMessage response =
            await HttpClient.PostWithBearerAsJsonAsync("api/v1.0/tags",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UpdateTag

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugContainsNonAsciiCharacters()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = UnicodeSlug };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_ReturnsUpdatedTag_WhenGivenValidData()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "UpdatedTag", Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
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
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = NameOverMaxLength, Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugIsTooLong()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = SlugOverMaxLength };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugIsInvalid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = InvalidSlug };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_LeavesNameUnchanged_WhenNameIsOmitted()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = null, Slug = "updated-tag-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
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
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "UpdatedName", Slug = null };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
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
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag tag = new Tag { Name = "OriginalName", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            new UpdateTagRequest(),
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns400_WhenSlugParamIsInvalid()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        UpdateTagRequest request = new UpdateTagRequest { Name = "tagName", Slug = "tag-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{InvalidSlug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTag_Returns404_WhenTagSlugNotFound()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = "new-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync("api/v1.0/tags/not-found-slug",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTag_Returns409_WhenSlugAlreadyExists()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Update]);
        Tag existingTag = new Tag { Name = "Existing", Slug = "existing-slug" };
        Tag otherTag = new Tag { Name = "Other", Slug = "other-tag" };
        await _tagsRepository.AddTag(existingTag);
        await _tagsRepository.AddTag(otherTag);
        UpdateTagRequest request = new UpdateTagRequest { Slug = otherTag.Slug };

        HttpResponseMessage response =
            await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{existingTag.Slug}",
                request,
                bearerToken,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateTag_Returns401_WhenNoBearerTokenIsProvided()
    {
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = "new-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync("api/v1.0/tags/tag-slug",
            request,
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTag_Returns403_WhenUserHasNoUpdatePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Read]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);
        UpdateTagRequest request = new UpdateTagRequest { Name = "TagName", Slug = "new-slug" };

        HttpResponseMessage response = await HttpClient.PutWithBearerAsJsonAsync($"api/v1.0/tags/{tag.Slug}",
            request,
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DeleteTag

    [Fact]
    public async Task DeleteTag_ReturnsDeletedTag_WhenGivenValidSlug()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Delete]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-to-delete" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync($"api/v1.0/tags/{tag.Slug}",
            bearerToken,
            TestContext.Current.CancellationToken);

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
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Delete]);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync($"api/v1.0/tags/{InvalidSlug}",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTag_Returns404_WhenSlugNotFound()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Delete]);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync("api/v1.0/tags/slug-not-found",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTag_Returns401_WhenNoBearerTokenIsProvided()
    {
        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync("api/v1.0/tags/tag-slug",
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTag_Returns403_WhenUserHasNoDeletePermission()
    {
        (_, string bearerToken) = await RegisterAuthenticatedUserWithPermissions([Permissions.Tags.Read]);
        Tag tag = new Tag { Name = "Tag", Slug = "tag-slug" };
        await _tagsRepository.AddTag(tag);

        HttpResponseMessage response = await HttpClient.DeleteWithBearerAsync($"api/v1.0/tags/{tag.Slug}",
            bearerToken,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}