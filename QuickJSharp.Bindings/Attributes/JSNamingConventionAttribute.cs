namespace QuickJSharp.Bindings;

/// <summary>
/// Specifies the default naming preference when things are exposed to JavaScript. By default, it's <see cref="NamingPreference.Original"/> for constructors and enum members, <see cref="NamingPreference.CamelCase"/> for members and other globals, and <see cref="NamingPreference.ScreamingSnakeCase"/> for constants.
/// </summary>
/// <remarks>
/// Only valid on the class you define for the source generator. Can be individually overridden.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class JSNamingConventionAttribute : Attribute
{
    /// <summary>
    /// The naming preference to apply to constructors when exposed to JavaScript. Also applied to namespace-like objects, including enums.
    /// </summary>
    public NamingPreference Constructors { get; set; } = NamingPreference.Original;

    /// <summary>
    /// The naming preference to apply to global functions and other non-constructor entities in globals.
    /// </summary>
    public NamingPreference Globals { get; set; } = NamingPreference.CamelCase;

    /// <summary>
    /// The naming preference to apply to enum members when exposed to JavaScript.
    /// </summary>
    public NamingPreference EnumMembers { get; set; } = NamingPreference.Original;

    /// <summary>
    /// The naming preference to apply to members (methods, properties, fields) when exposed to JavaScript.
    /// </summary>
    public NamingPreference Members { get; set; } = NamingPreference.CamelCase;

    /// <summary>
    /// The naming preference to apply to constants (const fields) when exposed to JavaScript.
    /// </summary>
    public NamingPreference Constants { get; set; } = NamingPreference.ScreamingSnakeCase;
}
