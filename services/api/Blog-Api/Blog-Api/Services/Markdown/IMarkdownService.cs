namespace BlogApi.Services.Markdown;

public interface IMarkdownService
{
    public string MarkdownToPlainText(string markdownText);
}