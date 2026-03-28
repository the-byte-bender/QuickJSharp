namespace QuickJSharp.Bindings;

/// <summary>
/// Indicates that something should be exposed to JavaScript.
/// </summary>
/// <remarks>
/// Setting this on a type (a class, struct, or enum) will register it with the runtime and add it to the global scope
/// for specific or all registries.
/// <para>
/// If <see cref="Registries"/> is empty, the item is exposed in all registries.
/// Otherwise, it is only exposed in the specified registry types.
/// </para>
/// <para>
/// For members, using this without arguments (e.g., <c>[JSExpose]</c>) exposes the member in all registries,
/// but since a member can only be registered if its containing type is also registered in that registry,
/// it is naturally scoped to whichever registries the containing type appears in.
/// This is good shorthand to avoid writing <c>typeof()</c> for every member when the intention is simply
/// to expose it alongside its containing type. Specifying explicit registries on a member further restricts
/// exposure to the intersection of those registries and the ones the containing type is exposed for.
/// </para>
/// <para>
/// For enums, all enum members are exposed by default.
/// </para>
/// <para>
/// For classes and structs, nothing is exposed by default, not even the constructor(s).
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Interface
        | AttributeTargets.Enum
        | AttributeTargets.Field
        | AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Constructor,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class JSExposeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JSExposeAttribute"/>.
    /// </summary>
    /// <param name="registries">
    /// Optional list of specific registry types (<see cref="BindingsRegistry"/>) this should be exposed for.
    /// If none are provided, it is exposed for all registries.
    /// </param>
    public JSExposeAttribute(params Type[] registries)
    {
        Registries = registries;
    }

    /// <summary>
    /// The specific registry types this item is exposed for.
    /// If empty, it is exposed for all registries.
    /// </summary>
    public Type[] Registries { get; }

    /// <summary>
    /// On fields and properties: Whether to expose a getter for this member.
    /// True by default.
    /// </summary>
    public bool Read { get; init; } = true;

    /// <summary>
    /// On fields and properties: Whether to expose a setter for this member.
    /// True by default.
    /// </summary>
    public bool Write { get; init; } = true;

    /// <summary>
    /// On class or struct that doesn't define any constructors: Whether to expose the implicit parameterless
    /// constructor. False by default.
    /// </summary>
    public bool ExposeImplicitConstructor { get; init; } = false;
}
