using System.Text;
using AwesomeAssertions;
using BlogApi.Services;

namespace BlogApi.Unit.Services;

public class TextServiceTests
{
    private const string LoremIpsum =
        "Lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore " +
        "et dolore magna aliqua Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut " +
        "aliquip ex ea commodo consequat Duis aute irure dolor in reprehenderit in voluptate velit esse " +
        "cillum dolore eu fugiat nulla pariatur Excepteur sint occaecat cupidatat non proident sunt in " +
        "culpa qui officia deserunt mollit anim id est laborum";

    private readonly TextService _textService;

    public TextServiceTests()
    {
        _textService = new TextService();
    }

    private static int CountWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    public void GetFirstWordsSubstring_ShouldReturnString_WithWordsCountEqualToSpecifiedAmount(int wordCount)
    {
        // Arrange
        int totalWords = CountWords(LoremIpsum);
        totalWords.Should().BeGreaterThanOrEqualTo(wordCount);

        // Act
        string substring = _textService.GetFirstWordsSubstring(LoremIpsum, wordCount);

        // Assert
        CountWords(substring).Should().Be(wordCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetFirstWordsSubstring_ShouldReturnAtLeastOneWord_WhenWordCountIsNotPositive(int wordCount)
    {
        // Act
        string substring = _textService.GetFirstWordsSubstring(LoremIpsum, wordCount);

        // Assert
        CountWords(substring).Should().Be(1);
    }

    [Fact]
    public void GetFirstWordsSubstring_ShouldReturnFullText_WhenWordCountExceedsTotalWords()
    {
        // Arrange
        int totalWords = CountWords(LoremIpsum);

        // Act
        string substring = _textService.GetFirstWordsSubstring(LoremIpsum, totalWords + 100);

        // Assert
        CountWords(substring).Should().Be(totalWords);
    }

    [Fact]
    public void GetFirstWordsSubstring_ShouldReturnEmptyString_WhenTextIsEmpty()
    {
        // Act
        string substring = _textService.GetFirstWordsSubstring("", 10);

        // Assert
        substring.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100, 1)]
    [InlineData(200, 1)]
    [InlineData(400, 2)]
    [InlineData(1000, 5)]
    public void EstimateReadingTime_ReturnsExpectedTime_ForGivenText(int wordCount, int expectedTime)
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < wordCount; ++i)
        {
            stringBuilder.Append($"word_{i}");
            if (i + 1 < wordCount)
            {
                stringBuilder.Append(", ");
            }
        }

        int readingTime = _textService.EstimateReadingTime(stringBuilder.ToString());

        readingTime.Should().Be(expectedTime);
    }

    [Fact]
    public void EstimateReadingTime_Returns1_ForEmptyText()
    {
        int readingTime = _textService.EstimateReadingTime("");

        readingTime.Should().Be(1);
    }
}