using System.Text;

namespace QuickJSharp.Bindings.Generators.Models;

enum NamingPreference
{
    Original,
    CamelCase,
    SnakeCase,
    ScreamingSnakeCase,
}

readonly record struct NamingConvention(
    NamingPreference Constructors = NamingPreference.Original,
    NamingPreference Globals = NamingPreference.CamelCase,
    NamingPreference EnumMembers = NamingPreference.Original,
    NamingPreference Members = NamingPreference.CamelCase,
    NamingPreference Constants = NamingPreference.ScreamingSnakeCase
);

static class NamingPreferenceAndConventionExtensions
{
    extension(NamingPreference preference)
    {
        public string Apply(string original)
        {
            if (string.IsNullOrWhiteSpace(original))
                return original;

            return preference switch
            {
                NamingPreference.Original => original,
                NamingPreference.CamelCase => ToCamelCase(original),
                NamingPreference.SnakeCase => ToSnakeCase(original),
                NamingPreference.ScreamingSnakeCase => ToSnakeCase(original).ToUpperInvariant(),
                _ => original,
            };
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            return name;

        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
                break;

            bool hasNext = (i + 1 < chars.Length);
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                break;

            chars[i] = char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    private static string ToSnakeCase(string text)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (i > 0 && char.IsUpper(c))
            {
                if (char.IsLower(text[i - 1]) || (i + 1 < text.Length && char.IsLower(text[i + 1])))
                {
                    sb.Append('_');
                }
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
