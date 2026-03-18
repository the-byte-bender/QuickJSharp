namespace QuickJSharp.Bindings;

/// <summary>
/// Defines how C# identifiers are transformed when exposed to JavaScript. This assumes the C# code follows standard .NET naming conventions (PascalCase for types and members).
/// </summary>
public enum NamingPreference
{
    /// <summary>
    /// Use the C# name as-is, with no modifications.
    /// </summary>
    Original,

    /// <summary>
    /// Convert the first character of the C# name to lowercase, leaving the rest unchanged.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Convert the C# name to snake_case, with all lowercase letters and underscores between words.
    /// </summary>
    SnakeCase,

    /// <summary>
    /// Convert the C# name to SCREAMING_SNAKE_CASE, with all uppercase letters and underscores between words.
    /// </summary>
    ScreamingSnakeCase
}
