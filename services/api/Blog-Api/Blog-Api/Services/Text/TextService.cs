using System.Text.RegularExpressions;

namespace BlogApi.Services.Text;

public partial class TextService : ITextService
{
    public string GetFirstWordsSubstring(string text, int wordCount)
    {
        wordCount = Math.Max(1, wordCount);

        string substring = text;
        Regex pattern = new Regex($@"(^(?:\S+\s*){{1,{wordCount}}}).*", RegexOptions.IgnoreCase);
        Match match = pattern.Match(substring);
        if (match.Success)
        {
            string firstWords = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(firstWords))
            {
                substring = firstWords.Trim();
            }
        }

        return substring;
    }

    public int EstimateReadingTime(string text)
    {
        Match match = ReadingTimeRegex().Match(text);
        int count = 0;
        while (match.Success)
        {
            count += 1;
            match = match.NextMatch();
        }

        // 200 words per minute
        return count > 0 ? Math.Max(count / 200, 1) : 1;
    }

    [GeneratedRegex(@"\S+")]
    private static partial Regex ReadingTimeRegex();
}