using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Domain;
using BlogApi.Repositories.Posts;
using BlogApi.Services.Comments;
using BlogApi.Services.Markdown;
using BlogApi.Services.Tags;
using BlogApi.Services.Text;
using BlogApi.Utils;

namespace BlogApi.Services.Posts;

public class PostsService : IPostsService
{
    private const int DescriptionWordCount = 50;
    private readonly ICommentsService _commentsService;
    private readonly IMarkdownService _markdownService;
    private readonly IPostsRepository _postsRepository;
    private readonly ITagsService _tagsService;
    private readonly ITextService _textService;

    public PostsService(IPostsRepository postsRepository, ITagsService tagsService, IMarkdownService markdownService,
        ITextService textService, ICommentsService commentsService)
    {
        _postsRepository = postsRepository;
        _tagsService = tagsService;
        _markdownService = markdownService;
        _textService = textService;
        _commentsService = commentsService;
    }

    public async Task<PagedPostsResult> GetPosts(GetPostsFilterQuery? filter, PaginationQuery? pagination)
    {
        return await _postsRepository.GetPosts(filter, pagination);
    }

    public async Task<Post?> GetPostBySlug(string slug)
    {
        return await _postsRepository.GetPostBySlug(slug);
    }

    public async Task<Post?> GetPostBySlugWithTags(string slug)
    {
        return await _postsRepository.GetPostBySlugWithTags(slug);
    }

    public async Task<Post?> GetPostBySlugWithComments(string slug)
    {
        return await _postsRepository.GetPostBySlugWithComments(slug);
    }

    public async Task<Post> CreatePost(Post post)
    {
        await _postsRepository.AddPost(post);
        return post;
    }

    public async Task UpdatePost(Post post, UpdatePostRequest updatePostDto)
    {
        string? body = updatePostDto.Body;
        if (body is not null)
        {
            post.Body = body;

            string bodyPlainText = _markdownService.MarkdownToPlainText(body);
            post.Description = _textService.GetFirstWordsSubstring(bodyPlainText, DescriptionWordCount) + "…";
            post.ReadingTime = _textService.EstimateReadingTime(bodyPlainText);
        }

        string? title = updatePostDto.Title;
        if (title is not null)
        {
            post.Title = title;
            post.Slug = await GenerateUniqueSlugAsync(title);
        }

        IReadOnlyCollection<string>? tagSlugs = updatePostDto.Tags;
        if (tagSlugs is not null)
        {
            // Replace tags
            List<Tag> tags = await _tagsService.GetAllTags(tagSlugs);
            post.Tags.Clear();
            foreach (Tag tag in tags)
            {
                post.Tags.Add(tag);
            }
        }

        bool? isPublished = updatePostDto.IsPublished;
        bool postIsAlreadyPublished = post.PublishedAt != null;
        if (isPublished is not null && isPublished != postIsAlreadyPublished)
        {
            if (isPublished is true)
            {
                post.PublishedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                post.PublishedAt = null;
            }
        }

        await _postsRepository.UpdatePost(post);
    }

    public async Task DeletePost(Post post)
    {
        await _postsRepository.DeletePost(post);
    }

    public async Task<string> GenerateUniqueSlugAsync(string title)
    {
        string baseSlug = SlugGenerator.Generate(title);

        IReadOnlyCollection<Post> matchingPosts = await _postsRepository.GetPostsStartingWithSlug(baseSlug);
        List<string> takenSlugs = matchingPosts.Select(p => p.Slug).ToList();

        if (takenSlugs.Count == 0)
        {
            return baseSlug;
        }

        int nextSuffix = takenSlugs
            .Where(slug => slug.Length > baseSlug.Length)
            .Select(slug => slug[(baseSlug.Length + 1)..])
            .Where(rest => rest.Length > 0 && rest.All(char.IsDigit))
            .Select(int.Parse)
            .DefaultIfEmpty(1)
            .Max() + 1;

        return $"{baseSlug}-{nextSuffix}";
    }

    public async Task<Comment> CreateCommentForPost(Post post, string username, string body)
    {
        return await _commentsService.CreateComment(username, body, post.Id);
    }
}