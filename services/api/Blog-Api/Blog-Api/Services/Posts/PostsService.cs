using BlogApi.Domain;
using BlogApi.Repositories.Posts;
using BlogApi.Utils;

namespace BlogApi.Services.Posts;

public class PostsService : IPostsService
{
    private readonly IPostsRepository _postsRepository;

    public PostsService(IPostsRepository postsRepository)
    {
        _postsRepository = postsRepository;
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