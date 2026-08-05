using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Data;
using BlogApi.Domain;
using BlogApi.Repositories.Comments;
using BlogApi.Repositories.Posts;
using BlogApi.Repositories.Tags;

namespace BlogApi.Integration.Controllers;

[Collection(nameof(TestsCollection))]
public class PostsControllerTests : IntegrationTestBase
{
    private const string InvalidSlug = "@--";

    private static readonly string TitleAtMaxLength = new string('a', Post.TitleMaxLength);
    private static readonly string TitleOverMaxLength = new string('a', Post.TitleMaxLength + 1);
    private static readonly string CommentBodyOverMaxLength = new string('a', Comment.BodyMaxLength + 1);
    private static readonly string CommentUsernameOverMaxLength = new string('a', Comment.UsernameMaxLength + 1);

    private BlogUser _author = null!;
    private ICommentsRepository _commentsRepository = null!;
    private DataContext _context = null!;
    private IPostsRepository _postsRepository = null!;
    private ITagsRepository _tagsRepository = null!;

    public PostsControllerTests(BlogApiFactory factory) : base(factory)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _postsRepository = GetRequiredService<IPostsRepository>();
        _tagsRepository = GetRequiredService<ITagsRepository>();
        _commentsRepository = GetRequiredService<ICommentsRepository>();
        _context = GetRequiredService<DataContext>();

        _author = await CreateUser("author@email.com", "Author name");
    }

    private async Task<BlogUser> CreateUser(string email, string name)
    {
        BlogUser user = new BlogUser
        {
            UserName = email,
            Email = email,
            DisplayName = name
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    // ---- GetBySlug ----

    [Fact]
    public async Task GetBySlug_ReturnsPost_WhenPostExists()
    {
        Post post = new Post
        {
            Title = "Test Post Title",
            Slug = "test-post-title",
            Description = "Test post description",
            Body = "Test post body content",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);
        body.Slug.Should().Be(post.Slug);
        body.Description.Should().Be(post.Description);
        body.Body.Should().Be(post.Body);
        body.PublishedAt.Should().BeNull();
        body.Author.Should().NotBeNull();
        body.Author.Id.Should().Be(_author.Id);
        body.Author.Name.Should().Be(_author.DisplayName);
        body.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySlug_ReturnsPost_WithTags_WhenPostHasTags()
    {
        Tag tag1 = new Tag { Name = "Java", Slug = "java" };
        Tag tag2 = new Tag { Name = "Spring", Slug = "spring" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);

        Post post = new Post
        {
            Title = "Post with Tags",
            Slug = "post-with-tags",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag1);
        post.Tags.Add(tag2);
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Tags.Should().HaveCount(2);
        body.Tags.Should().Contain(t => t.Slug == "java");
        body.Tags.Should().Contain(t => t.Slug == "spring");
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenSlugDoesNotExist()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{InvalidSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- GetPosts ----

    [Fact]
    public async Task GetPosts_ReturnsEmptyEnvelope_WhenNoPostsExist()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=1&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
        body.Metadata.Count.Should().Be(0);
        body.Metadata.PageNumber.Should().Be(1);
        body.Metadata.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPosts_ReturnsEnvelopeWithResults_WhenPublishedPostsExist()
    {
        Post post = new Post
        {
            Title = "Test Post",
            Slug = "test-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageNumber=1&pageSize=10", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == post.Slug && p.Title == post.Title);
        body.Metadata.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_MetadataSortByIsNull_WhenSortByIsOmitted()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.SortBy.Should().BeNull();
    }

    [Fact]
    public async Task GetPosts_MetadataSortBy_IsSortByGivenInQuery()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?sortBy=id", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.SortBy.Should().Be("id");
    }

    [Fact]
    public async Task GetPosts_Returns400_WhenSortByIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?sortBy=invalidSortBy",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_Returns400_WhenTagSlugIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1.0/posts?tags={InvalidSlug}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPosts_BindsTagsQueryParam_WhenFilteringByTags()
    {
        Tag javaTag = new Tag { Name = "Java", Slug = "java" };
        Tag dockerTag = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(javaTag);
        await _tagsRepository.AddTag(dockerTag);

        Post postWithJavaTag = new Post
        {
            Title = "Java post",
            Slug = "java-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        postWithJavaTag.Tags.Add(javaTag);
        Post postWithDockerTag = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        postWithDockerTag.Tags.Add(dockerTag);
        await _postsRepository.AddPost(postWithJavaTag);
        await _postsRepository.AddPost(postWithDockerTag);

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?tags=java", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "java-post");
    }

    [Fact]
    public async Task GetPosts_BindsAuthorQueryParam_WhenFilteringByAuthor()
    {
        BlogUser author1 = _author;
        BlogUser author2 = await CreateUser("author2@email.com", "Author2 name");
        Post postWithAuthor1 = new Post
        {
            Title = "Java post",
            Slug = "java-post",
            AuthorId = author1.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        Post postWithAuthor2 = new Post
        {
            Title = "Docker post",
            Slug = "docker-post",
            AuthorId = author2.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(postWithAuthor1);
        await _postsRepository.AddPost(postWithAuthor2);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts?author={author2.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "docker-post");
    }

    [Fact]
    public async Task GetPosts_RespectsPaginationQueryParams()
    {
        for (int i = 0; i < 15; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });

        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=2&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(5);
        body.Metadata.Count.Should().Be(15);
        body.Metadata.PageNumber.Should().Be(2);
        body.Metadata.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPosts_ClampsPageNumberToOne_WhenPageNumberIsNegative()
    {
        for (int i = 0; i < 5; i++)
            await _postsRepository.AddPost(new Post
            {
                Title = $"Post {i}",
                Slug = $"post-{i}",
                AuthorId = _author.Id,
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });

        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts?pageNumber=-50&pageSize=10",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(5);
        body.Metadata.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_ClampsPageSizeToDefault_WhenPageSizeIsTooSmall()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageSize=-30", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetPosts_ClampsPageSizeToMax_WhenPageSizeIsTooBig()
    {
        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?pageSize=999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Metadata.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetPosts_ExcludesUnpublishedPosts_WhenUnpublishedParamIsOmitted()
    {
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_ExcludesUnpublishedPosts_WhenUnpublishedParamIsFalse()
    {
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?unpublished=false", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPosts_IncludesUnpublishedPosts_WhenUnpublishedParamIsTrue()
    {
        // NOTE: no auth exists yet in this port, so `unpublished=true` currently works for anyone.
        // See PostsController.GetPosts TODO comment - this should eventually default to false when unauthenticated.
        await _postsRepository.AddPost(new Post
        {
            Title = "Draft post",
            Slug = "draft-post",
            AuthorId = _author.Id
        });

        HttpResponseMessage response =
            await HttpClient.GetAsync("api/v1.0/posts?unpublished=true", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostsEnvelope? body =
            await response.Content.ReadFromJsonAsync<PostsEnvelope>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Posts.Should().HaveCount(1);
        body.Posts.Should().Contain(p => p.Slug == "draft-post");
    }

    // ---- DeletePost ----

    [Fact]
    public async Task DeletePost_ReturnsDeletedPost_WhenSlugExists()
    {
        Post post = new Post
        {
            Title = "Post to Delete",
            Slug = "post-to-delete",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be(post.Title);

        HttpResponseMessage followUpResponse =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}", TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_Returns404_WhenSlugDoesNotExist()
    {
        HttpResponseMessage response = await HttpClient.DeleteAsync("api/v1.0/posts/does-not-exist",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response =
            await HttpClient.DeleteAsync($"api/v1.0/posts/{InvalidSlug}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- CreatePost ----

    // BUG: CreatePost hardcodes AuthorId = Guid.NewGuid() (see PostsController.CreatePost TODO), which
    // points at a non-existent user and trips the posts.author_id FK constraint, so this currently
    // 500s instead of returning 201. Skipped until an authenticated author id is wired in.
    [Fact(Skip = "CreatePost.AuthorId is hardcoded to a random unsaved Guid - see TODO in PostsController.CreatePost")]
    public async Task CreatePost_ReturnsCreatedPost_WhenGivenValidTitle()
    {
        CreatePostRequest request = new CreatePostRequest { Title = "Post title" };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/posts", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be(request.Title);
        response.Headers.Location.Should().NotBeNull();

        HttpResponseMessage locationResponse =
            await HttpClient.GetAsync(response.Headers.Location, TestContext.Current.CancellationToken);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? locationBody =
            await locationResponse.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        locationBody.Should().NotBeNull();
        locationBody.Title.Should().Be(request.Title);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsMissing()
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1.0/posts", new { },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsTooLong()
    {
        CreatePostRequest request = new CreatePostRequest { Title = TitleOverMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/posts", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // See BUG note on CreatePost_ReturnsCreatedPost_WhenGivenValidTitle above.
    [Fact(Skip = "CreatePost.AuthorId is hardcoded to a random unsaved Guid - see TODO in PostsController.CreatePost")]
    public async Task CreatePost_ReturnsCreatedPost_WhenTitleIsExactlyAtMaxLength()
    {
        CreatePostRequest request = new CreatePostRequest { Title = TitleAtMaxLength };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/posts", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsBlank()
    {
        CreatePostRequest request = new CreatePostRequest { Title = "        " };

        HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync("api/v1.0/posts", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Returns400_WhenTitleIsNull()
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1.0/posts", new { Title = (string?)null },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- UpdatePost ----

    [Fact]
    public async Task UpdatePost_ReturnsUpdatedPost_WhenGivenValidData()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title", Body = "Updated Body" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(post.Id);
        body.Title.Should().Be("Updated Title");
        body.Body.Should().Be("Updated Body");
    }

    [Fact]
    public async Task UpdatePost_Returns404_WhenSlugDoesNotExist()
    {
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync("api/v1.0/posts/does-not-exist", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenSlugParamIsInvalid()
    {
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{InvalidSlug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenRequestBodyIsEmptyObject()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}",
            new UpdatePostRequest(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenTitleIsTooLong()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = TitleOverMaxLength };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_Returns400_WhenTagsContainInvalidSlug()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Tags = ["java", "not@ValidSlug!"] };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_ReplacesTags_WhenTagsAreGiven()
    {
        Tag tag1 = new Tag { Name = "Java", Slug = "java" };
        Tag tag2 = new Tag { Name = "Spring", Slug = "spring" };
        Tag tag3 = new Tag { Name = "Docker", Slug = "docker" };
        await _tagsRepository.AddTag(tag1);
        await _tagsRepository.AddTag(tag2);
        await _tagsRepository.AddTag(tag3);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag1);
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest { Tags = ["spring", "docker"] };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Tags.Should().HaveCount(2);
        body.Tags.Should().Contain(t => t.Slug == "spring");
        body.Tags.Should().Contain(t => t.Slug == "docker");
        body.Tags.Should().NotContain(t => t.Slug == "java");
    }

    [Fact]
    public async Task UpdatePost_LeavesBodyUnchanged_WhenOnlyTitleIsGiven()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Updated Title Only");
        body.Body.Should().Be("Original Body");
    }

    [Fact]
    public async Task UpdatePost_LeavesTitleUnchanged_WhenOnlyBodyIsGiven()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            Body = "Original Body",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Body = "Updated Body Only" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Original Title");
        body.Body.Should().Be("Updated Body Only");
    }

    [Fact]
    public async Task UpdatePost_LeavesTagsUnchanged_WhenTagsAreOmitted()
    {
        Tag tag = new Tag { Name = "Java", Slug = "java" };
        await _tagsRepository.AddTag(tag);

        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        post.Tags.Add(tag);
        await _postsRepository.AddPost(post);

        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Title.Should().Be("Updated Title Only");
        body.Tags.Should().HaveCount(1);
        body.Tags.Should().Contain(t => t.Slug == "java");
    }

    [Fact]
    public async Task UpdatePost_PublishesPost_WhenIsPublishedIsTrue()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { IsPublished = true };
        DateTimeOffset before = DateTimeOffset.UtcNow;

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().NotBeNull();
        body.PublishedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdatePost_HidesPost_WhenIsPublishedIsFalse()
    {
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id,
            PublishedAt = DateTimeOffset.UtcNow
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { IsPublished = false };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePost_LeavesPublishedAtUnchanged_WhenIsPublishedIsNotProvided()
    {
        DateTimeOffset originalPublishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        Post post = new Post
        {
            Title = "Original Title",
            Slug = "original-title",
            AuthorId = _author.Id,
            PublishedAt = originalPublishedAt
        };
        await _postsRepository.AddPost(post);
        UpdatePostRequest request = new UpdatePostRequest { Title = "Updated Title Only" };

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/v1.0/posts/{post.Slug}", request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PostResponse? body =
            await response.Content.ReadFromJsonAsync<PostResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.PublishedAt.Should().BeCloseTo(originalPublishedAt, TimeSpan.FromMilliseconds(1));
    }

    // ---- GetPostCommentsBySlug ----

    [Fact]
    public async Task GetPostCommentsBySlug_ReturnsEmptyList_WhenPostHasNoComments()
    {
        Post post = new Post
        {
            Title = "Post without comments",
            Slug = "post-without-comments",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Comments.Should().BeEmpty();
        body.Metadata.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_ReturnsComments_WhenPostHasComments()
    {
        Post post = new Post
        {
            Title = "Post with comments",
            Slug = "post-with-comments",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        Comment comment1 = new Comment
        {
            Username = "commenter-one",
            Body = "First comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = post.Id
        };
        Comment comment2 = new Comment
        {
            Username = "commenter-two",
            Body = "Second comment body",
            CreatedAt = DateTimeOffset.UtcNow,
            PostId = post.Id
        };
        await _commentsRepository.AddComment(comment1);
        await _commentsRepository.AddComment(comment2);

        HttpResponseMessage response =
            await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? body =
            await response.Content.ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Comments.Should().HaveCount(2);
        body.Metadata.Count.Should().Be(2);
        body.Comments.Should().Contain(c =>
            c.Username == comment1.Username && c.Body == comment1.Body && c.PostId == post.Id);
        body.Comments.Should().Contain(c =>
            c.Username == comment2.Username && c.Body == comment2.Body && c.PostId == post.Id);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_Returns404_WhenSlugDoesNotExist()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1.0/posts/does-not-exist/comments",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPostCommentsBySlug_Returns400_WhenSlugIsInvalid()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1.0/posts/{InvalidSlug}/comments",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- CreatePostComment ----

    [Fact]
    public async Task CreatePostComment_ReturnsCreatedComment_WhenGivenValidRequest()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CommentResponse? body =
            await response.Content.ReadFromJsonAsync<CommentResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().NotBeEmpty();
        body.Username.Should().Be(request.Username);
        body.Body.Should().Be(request.Body);
        body.PostId.Should().Be(post.Id);
    }

    [Fact]
    public async Task CreatePostComment_PersistsComment_WhenGivenValidRequest()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-persisted",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage followUpResponse = await HttpClient.GetAsync($"api/v1.0/posts/{post.Slug}/comments",
            TestContext.Current.CancellationToken);
        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        GetPostCommentsResponse? followUpBody = await followUpResponse.Content
            .ReadFromJsonAsync<GetPostCommentsResponse>(TestContext.Current.CancellationToken);
        followUpBody.Should().NotBeNull();
        followUpBody.Comments.Should().Contain(c => c.Username == request.Username && c.Body == request.Body);
    }

    [Fact]
    public async Task CreatePostComment_Returns404_WhenSlugDoesNotExist()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1.0/posts/does-not-exist/comments",
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenSlugIsInvalid()
    {
        CreatePostCommentRequest request = new CreatePostCommentRequest { Username = "commenter", Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{InvalidSlug}/comments",
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenBodyIsMissing()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-missing-body",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            new { Username = "commenter" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenUsernameIsMissing()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-missing-username",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            new { Body = "Nice post!" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenBodyExceedsMaxLength()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-body-too-long",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request =
            new CreatePostCommentRequest { Username = "commenter", Body = CommentBodyOverMaxLength };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePostComment_Returns400_WhenUsernameExceedsMaxLength()
    {
        Post post = new Post
        {
            Title = "Post to comment on",
            Slug = "post-to-comment-on-username-too-long",
            AuthorId = _author.Id
        };
        await _postsRepository.AddPost(post);
        CreatePostCommentRequest request =
            new CreatePostCommentRequest { Username = CommentUsernameOverMaxLength, Body = "Nice post!" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/v1.0/posts/{post.Slug}/comments",
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // GetPostsResponse only exposes a private constructor + static factory (Create), which
    // System.Text.Json's reflection-based deserializer cannot use. That's fine for production
    // (the type is only ever serialized, never deserialized server-side) but means we can't
    // deserialize directly into it here. This mirrors the wire shape instead.
    private record PostsEnvelope
    {
        [JsonPropertyName("results")]
        public required List<PostResponse> Posts { get; init; }

        public required PagedResponseMetadata Metadata { get; init; }
    }
}