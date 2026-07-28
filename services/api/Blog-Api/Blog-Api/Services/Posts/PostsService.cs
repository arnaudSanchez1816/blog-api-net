using BlogApi.Contracts.V1.Requests;
using BlogApi.Domain;
using BlogApi.Repositories.Posts;
using BlogApi.Services.Tags;
using BlogApi.Utils;

namespace BlogApi.Services.Posts;

public class PostsService : IPostsService
{
    private readonly IPostsRepository _postsRepository;
    private readonly ITagsService _tagsService;

    public PostsService(IPostsRepository postsRepository, ITagsService tagsService)
    {
        _postsRepository = postsRepository;
        _tagsService = tagsService;
    }

    public async Task<Post?> GetPostBySlug(string slug)
    {
        return await _postsRepository.GetPostBySlug(slug);
    }

    public async Task<Post?> GetPostBySlugWithTags(string slug)
    {
        return await _postsRepository.GetPostBySlugWithTags(slug);
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
            // Todo : sanitize body
            post.Body = body;
            // Todo : parse description plain text from markdown body
            post.Description = body.Substring(0, Math.Min(body.Length, 50));
            // Todo : estimate reading time
            post.ReadingTime = 1;
        }

        string? title = updatePostDto.Title;
        if (title is not null)
        {
            // Todo : sanitize body
            post.Title = title;
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
            .Select(slug => slug[(baseSlug.Length + 1)..])
            .Where(rest => rest.Length > 0 && rest.All(char.IsDigit))
            .Select(int.Parse)
            .DefaultIfEmpty(1)
            .Max() + 1;

        return $"{baseSlug}-{nextSuffix}";
    }
}