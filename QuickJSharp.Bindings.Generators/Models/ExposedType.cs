using System.Collections.Immutable;
using System.Linq;
using QuickJSharp.Bindings.Generators.Infrastructure;

namespace QuickJSharp.Bindings.Generators.Models;

enum ExposedTypeKind
{
    Class,
    Struct,
    Interface,
    Enum,
}

readonly record struct ExposedType(
    // The full .NET type name that is safe to use in generated code, with global:: prefix any and generic parameters
    // fully specified. This is the source of truth for the .NET type in code generation.
    string DotnetType,
    string DotnetName,
    ExposedTypeKind Kind,
    EquatableArray<ExposedMember> Members,
    string? BaseType = null,
    bool ExposeImplicitConstructor = false,
    bool IsStatic = false,
    // A list of fully qualified .NET type names for registries that this type should be exposed for. If null,
    // it is exposed for all registries. Should be safe for use in generated code as is, with global:: prefix.
    EquatableArray<string>? ExposedFor = null,
    // The JavaScript name for the type if overridden. If the naming preference is overridden, this will be null in
    // the first pass and the JS name will be derived from the .NET name using the overridden convention.
    // Guaranteed to be not null in the final model after processing against context, and it should always be used as
    // the source of truth for the JS name in code generation.
    string? JSName = null,
    NamingPreference? OverriddenNamingPreference = null
)
{
    public ExposedType ResolveAgainstRegistry(Registry registry)
    {
        var kind = Kind;
        var jsName = "";
        var members = new EquatableArray<ExposedMember>(
            Members
                .GetInner()
                .Where(m =>
                    m.ExposedFor is null
                    || m.ExposedFor.Value.IsEmpty
                    || m.ExposedFor.Value.GetInner().Contains(registry.DotnetType)
                )
                .Select(m => m.ResolveAgainstRegistry(registry, kind))
                .ToImmutableArray()
        );

        if (JSName is not null)
            jsName = JSName;
        else if (OverriddenNamingPreference.HasValue)
            jsName = OverriddenNamingPreference.Value.Apply(DotnetName);
        else
            jsName = registry.NamingConvention.Constructors.Apply(DotnetName);
        return this with { JSName = jsName, Members = members };
    }
}
