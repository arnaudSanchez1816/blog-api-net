namespace BlogApi.Services.Text;

public interface ITextService
{
    public string GetFirstWordsSubstring(string text, int wordCount);

    public int EstimateReadingTime(string text);
}