namespace QuickJSharp.Bindings;

/// <summary>
/// Indicates that something should be exposed to JavaScript in all marked registries.
/// </summary>
/// <remarks>
/// Setting this on a type (a class, struct, or enum) will register it with the runtime and add it to the global scope
/// for contexts.
/// For class or struct, this means the constructor function if any constructor is exposed, otherwise a namespace-like.
/// For enums, this means the namespace-like object that contains the enum members.
/// Any exposed statics on the type will be added as properties to that global.
/// <para>
/// For enums, all enum members are exposed by default.
/// For classes and structs, nothing is exposed by default, not even the constructor(s).
/// You must explicitly mark members with this attribute to expose them, and you must mark at least one constructor
/// if you want the type to be constructible from JavaScript.
/// </para>
/// <para>
/// For structs and classes without a C# constructor defined, see <see cref="ExposeImplicitConstructor"/>
/// on this attribute.
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
    /// On fields and properties: Whether to expose a getter for this member.
    /// Set false for write-only access to something that has a getter in C# you want to hide.
    /// True by default.
    /// </summary>
    /// <remarks>
    /// This will always be assumed false if it's on a property that doesn't have a getter, so you don't have to
    /// explicitly set it to false in that case.
    /// </remarks>
    public bool Read { get; init; } = true;

    /// <summary>
    /// On fields and properties: Whether to expose a setter for this member.
    /// Set false for read-only access to something that has a setter in C# you want to hide.
    /// True by default.
    /// </summary>
    /// <remarks>
    /// This will always be assumed false if it's on a property that doesn't have a setter,
    /// or if it's on a field marked readonly, so you don't have to explicitly set it to false in those cases.
    /// </remarks>
    public bool Write { get; init; } = true;

    /// <summary>
    /// On class or struct that  doesn't define any constructors: Whether to expose the implicit parameterless
    /// constructor. False by default.
    /// </summary>
    /// <remarks>
    /// This only applies to classes or structs that don't define any constructors at all.
    /// If there is at least one constructor defined, whether parameterless or not, then this property has no effect
    /// and the default visibility rules for constructors apply (i.e. you must explicitly mark any constructor
    /// you want to expose with [JSExpose] regardless of the value of this property).
    /// </remarks>
    public bool ExposeImplicitConstructor { get; init; } = false;
}

/// <summary>
/// Indicates that something should be exposed to JavaScript in a specific registry.
/// You can use multiple of these on the same item to expose it in multiple specific registries.
/// </summary>
/// <remarks>
/// Behaves identically to <see cref="JSExposeAttribute"/> in all respects except that exposure is limited to
/// <typeparamref name="TRegistry"/> only. The type or member will not appear in any other registry.
/// <para>
/// Useful for cases where you want to have different bindings for different registries to make runtimes with, like a
/// "main" registry for general use and a "worker" registry for things that should only be exposed in web workers.
/// </para>
/// <para>
/// If an item is marked with both <see cref="JSExposeAttribute"/> and <see cref="JSExposeForAttribute{TRegistry}"/>,
/// only the <see cref="JSExposeAttribute"/> will have an effect, and the item will be exposed in all registries.
/// </para>
/// <para>
/// If the containing type (class / struct) is marked with <see cref="JSExposeForAttribute{TRegistry}"/>, the item
/// will only be exposed in the specified registry and you can safely use the shorter <see cref="JSExposeAttribute"/>
/// on members of that type without worrying about accidentally exposing them in other registries, since
/// the containing type itself won't be registered with them in the firstplace.
/// </para>
/// <para>
/// You can make the containing type global to all registries with <see cref="JSExposeAttribute"/> and then use
/// <see cref="JSExposeForAttribute{TRegistry}"/> on some members to limit their exposure to specific registries
/// if you want to give different registries special views of the same type.
/// </para>
/// </remarks>
/// <seealso cref="JSExposeAttribute"/>
/// <seealso cref="JSNameAttribute"/>
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
    AllowMultiple = true
)]
public sealed class JSExposeForAttribute<TRegistry> : Attribute
    where TRegistry : BindingExtension
{
    /// <inheritdoc cref="JSExposeAttribute.Read"/>
    public bool Read { get; init; } = true;

    /// <inheritdoc cref="JSExposeAttribute.Write"/>
    public bool Write { get; init; } = true;

    /// <inheritdoc cref="JSExposeAttribute.ExposeImplicitConstructor"/>
    public bool ExposeImplicitConstructor { get; init; } = false;
}
