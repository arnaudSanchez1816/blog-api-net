using System.Text.RegularExpressions;
using AwesomeAssertions;
using BlogApi.Utils;

namespace BlogApi.Unit.Utils;

public partial class SlugGeneratorTests
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private partial Regex IsSlugRegex();

    [Theory]
    [InlineData("This is an input", "this-is-an-input")]
    [InlineData("ALL UPPERCASE LETTER", "all-uppercase-letter")]
    [InlineData("MiX Of Both", "mix-of-both")]
    [InlineData("special chars!", "special-chars")]
    [InlineData("Two  spaces", "two-spaces")]
    public void SlugGenerator_Generate_ReturnsExpectedSlug(string input, string expected)
    {
        string result = SlugGenerator.Generate(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SlugGenerator_Generate_ReturnsValidSlug()
    {
        const string input = "random input string with words.";
        string result = SlugGenerator.Generate(input);
        result.Should().MatchRegex(IsSlugRegex());
    }

    [Fact]
    public void SlugGenerator_Generate_ReturnsValidSlugWhenInputContainsMultipleDashes()
    {
        const string input = "Input string --- other input string";
        string result = SlugGenerator.Generate(input);
        result.Should().MatchRegex(IsSlugRegex());
    }

    [Fact]
    public void SlugGenerator_Generate_ShouldTrimDashes()
    {
        const string input = "---slug name---";
        string result = SlugGenerator.Generate(input);
        result.Should().MatchRegex(IsSlugRegex());
        result.Should().Be("slug-name");
    }

    [Fact]
    public void SlugGenerator_Generate_ShouldBeLessOrEqualToMaxSlugLength()
    {
        string input = new string('a', SlugGenerator.MaxSlugLength + 50);
        string result = SlugGenerator.Generate(input);
        result.Should().MatchRegex(IsSlugRegex());
        result.Should().HaveLength(SlugGenerator.MaxSlugLength);
    }

    [Fact]
    public void SlugGenerator_Generate_ShouldThrowIfInputIsEmpty()
    {
        const string input = "";
        Action act = () => SlugGenerator.Generate(input);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SlugGenerator_Generate_ShouldThrowIfInputIsOnlyDashes()
    {
        const string input = "------";
        Action act = () => SlugGenerator.Generate(input);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SlugGenerator_Generate_ShouldThrowIfInputIsOnlySpaces()
    {
        const string input = "      \t\r\n";
        Action act = () => SlugGenerator.Generate(input);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().NotBeNullOrEmpty();
    }
}