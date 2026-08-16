namespace BlogApi.Exceptions;

public class SlugConflictException : Exception
{
    public string Slug { get; init; }

    public SlugConflictException(string slug, Exception? innerException = null) : base(
        $"Given slug is already used : {slug}",
        innerException)
    {
        Slug = slug;
    }
}