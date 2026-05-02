using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlogCMS.Business.Helpers;

public static partial class SlugGenerator
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("N")[..8];

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        slug = NonWord().Replace(slug, "-");
        slug = MultiDash().Replace(slug, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }

    public static string GenerateUnique(string input, Func<string, bool> exists)
    {
        var baseSlug = Generate(input);
        var slug = baseSlug;
        var i = 2;
        while (exists(slug)) slug = $"{baseSlug}-{i++}";
        return slug;
    }

    public static async Task<string> GenerateUniqueAsync(string input, Func<string, Task<bool>> exists)
    {
        var baseSlug = Generate(input);
        var slug = baseSlug;
        var i = 2;
        while (await exists(slug)) slug = $"{baseSlug}-{i++}";
        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+")] private static partial Regex NonWord();
    [GeneratedRegex(@"-+")]         private static partial Regex MultiDash();
}
