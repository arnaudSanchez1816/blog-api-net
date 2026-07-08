using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlogApi.Utils;

public static class SlugGenerator
{
    private const int MaxSlugLength = 200;

    public static string Slugify(string input)
    {
        string normalized = input.Normalize(NormalizationForm.FormD);

        StringBuilder builder = new StringBuilder();
        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char lower = char.ToLowerInvariant(c);
            if (char.IsLetterOrDigit(lower) && lower <= 'z')
            {
                builder.Append(lower);
            }
            else
            {
                builder.Append('-');
            }
        }

        string slug = Regex.Replace(builder.ToString(), "-+", "-").Trim('-');

        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength].TrimEnd('-');
        }

        return slug.Length == 0 ? $"post-{Guid.NewGuid():N}"[..12] : slug;
    }
}