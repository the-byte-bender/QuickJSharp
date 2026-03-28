using QuickJSharp.Bindings.Generators.Infrastructure;

namespace QuickJSharp.Bindings.Generators.Models;

enum ExposedMemberKind
{
    Field,
    Const,
    Property,
    Method,
    Constructor,
    Event,
}

/// <summary>
/// Represents a .NET member exposed to JavaScript.
/// This acts as a union of all possible exposed member metadata.
/// </summary>
/// <param name="DotnetName">The name of the member in .NET.</param>
/// <param name="Kind">The kind of member this represents.</param>
/// <param name="IsStatic">Whether the member is static at the .NET level.</param>
/// <param name="IsOverride">Whether the member is an override of a base class member.</param>
/// <param name="JSName">
/// The name of the member as it will appear in JavaScript.
/// Guaranteed to be not null in the final model after processing against context.
/// </param>
/// <param name="OverriddenNamingPreference">An optional override for the naming convention of this member.</param>
/// <param name="Parameters">
/// The parameters for a <see cref="ExposedMemberKind.Method"/> or <see cref="ExposedMemberKind.Constructor"/>.
/// Should be an empty array for other member kinds.
/// </param>
/// <param name="ReturnType">
/// The return type information.
/// Valid for <see cref="ExposedMemberKind.Method"/>, <see cref="ExposedMemberKind.Property"/> (getter),
/// or <see cref="ExposedMemberKind.Field"/>.
/// For <see cref="ExposedMemberKind.Constructor"/>, this is typically the type it constructs.
/// </param>
/// <param name="IsRefReturn">Whether the method returns by reference. For properties, this indicates a ref return
/// property.</param>
/// <param name="HasGetter">Valid for <see cref="ExposedMemberKind.Property"/>. Whether the property has a getter.
/// </param>
/// <param name="GetterMethodName">
/// The name of the underlying getter method in .NET.
/// Only valid when <paramref name="HasGetter"/> is true.
/// </param>
/// <param name="HasSetter">Valid for <see cref="ExposedMemberKind.Property"/>. Whether the property has a setter.
/// </param>
/// <param name="SetterMethodName">
/// The name of the underlying setter method in .NET.
/// Only valid when <paramref name="HasSetter"/> is true.
/// </param>
/// <param name="IsReadOnly">Valid for <see cref="ExposedMemberKind.Field"/> or
/// <see cref="ExposedMemberKind.Property"/>.</param>
/// <param name="ExposedFor">
/// A list of fully qualified .NET type names for registries that this member should be exposed for.
/// If null, it is exposed for all registries specified on the containing type.
/// Should be safe for use in generated code as is, with global:: prefix.
/// </param>
readonly record struct ExposedMember(
    string DotnetName,
    ExposedMemberKind Kind,
    bool IsStatic = false,
    bool IsOverride = false,
    string? JSName = null,
    NamingPreference? OverriddenNamingPreference = null,
    EquatableArray<ExposedParameter> Parameters = default,
    TypeInfo? ReturnType = null,
    bool IsRefReturn = false,
    bool HasGetter = false,
    string? GetterMethodName = null,
    bool HasSetter = false,
    string? SetterMethodName = null,
    bool IsReadOnly = false,
    EquatableArray<string>? ExposedFor = null
)
{
    public ExposedMember ResolveAgainstRegistry(Registry registry, ExposedTypeKind parentKind)
    {
        if (JSName is not null)
        {
            return this;
        }

        var preference =
            OverriddenNamingPreference
            ?? (
                parentKind == ExposedTypeKind.Enum
                    ? registry.NamingConvention.EnumMembers
                    : Kind switch
                    {
                        ExposedMemberKind.Constructor => registry.NamingConvention.Constructors,
                        ExposedMemberKind.Const => registry.NamingConvention.Constants,
                        _ => registry.NamingConvention.Members,
                    }
            );

        return this with
        {
            JSName = preference.Apply(DotnetName),
        };
    }
}
