namespace QuickJSharp.Bindings.Generators.Models;

/// <summary>
/// Represents a .NET type with metadata relevant for JavaScript binding generation.
/// </summary>
public readonly record struct TypeInfo(
    /// <summary>
    /// The fully qualified .NET type name, including the global:: prefix.
    /// </summary>
    string DotnetType,
    /// <summary>
    /// Indicates if the type is an enumeration.
    /// </summary>
    bool IsEnum = false,
    /// <summary>
    /// If <see cref="IsEnum"/> is true, this is the fully qualified name of the underlying numeric type.
    /// </summary>
    string? UnderlyingType = null
)
{
    public override string ToString() => DotnetType;
}
