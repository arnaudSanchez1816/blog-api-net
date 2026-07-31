namespace BlogApi.Services.Markdown;

public class MarkdownService : IMarkdownService
{
    public string MarkdownToPlainText(string markdownText)
    {
        return Markdig.Markdown.ToPlainText(markdownText);
    }
}