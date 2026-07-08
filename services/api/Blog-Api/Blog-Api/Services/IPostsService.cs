namespace BlogApi.Services;

public interface IPostsService
{
    public Task<string> GenerateUniqueSlugAsync(string title);
}