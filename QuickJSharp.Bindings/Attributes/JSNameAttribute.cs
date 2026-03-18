namespace QuickJSharp.Bindings;

/// <summary>
/// Specifies either a convention override or an explicit name when exposed to JavaScript. This will take precedence
/// over any default naming convention specified by <see cref="JSNamingConventionAttribute"/> or the source
/// generator's defaults for this entity.
/// </summary>
/// <remarks>
/// If applied to a class or struct, this will specify the name of the constructor function in JavaScript.
/// When defined to rename a class, attribute inheritence for the attribute on the class will be ignored to prevent
/// child classes from having the same js name as their parent.
/// If applied to an enum, this will specify the name of the enum type (namespace-like object), but not its members.
/// Use on individual members to rename those instead.
/// If applied to a method, property, or field, this will specify the name of that member in JavaScript.
/// <para>
/// If constructed with a string, that string will be used as the name in JavaScript directly.
/// If constructed with a <see cref="NamingPreference"/>, that naming convention will be applied to the C# name
/// to determine the JavaScript name.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Interface
        | AttributeTargets.Enum
        | AttributeTargets.Field
        | AttributeTargets.Property
        | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = false
)]
public sealed class JSNameAttribute : Attribute
{
    /// <summary>
    /// The name to use for this entity when exposed to JavaScript. Overrides any default naming convention.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The naming convention to apply to this entity when exposed to JavaScript. Overrides any default
    /// naming convention. Ignored if <see cref="Name"/> is set.
    /// </summary>
    public NamingPreference? Convention { get; }

    /// <summary>
    /// Creates a new <see cref="JSNameAttribute"/> with an explicit name to use in JavaScript.
    /// </summary>
    /// <param name="name"></param>
    public JSNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new <see cref="JSNameAttribute"/> with a naming convention.
    /// </summary>
    /// <param name="convention"></param>
    /// <remarks>
    /// The specified convention will be applied to the C# name of the entity to determine the JavaScript name.
    /// </remarks>
    public JSNameAttribute(NamingPreference convention)
    {
        Convention = convention;
    }
}
