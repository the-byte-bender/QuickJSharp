using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using QuickJSharp.Bindings.Generators.Models;

namespace QuickJSharp.Bindings.Generators.Extraction;

static class RegistryExtractor
{
    public static Registry? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;

        var currentBase = symbol.BaseType;
        bool inheritsFromBindingExtension = false;
        while (currentBase is not null)
        {
            if (currentBase.ToDisplayString() == BindingConstants.BindingExtensionBaseType)
            {
                inheritsFromBindingExtension = true;
                break;
            }
            currentBase = currentBase.BaseType;
        }

        if (!inheritsFromBindingExtension)
            return null;

        bool hasPartialCtor = symbol.InstanceConstructors.Any(c =>
            c.Parameters.IsEmpty && c.IsPartialDefinition && c.DeclaredAccessibility == Accessibility.Public
        );

        if (!hasPartialCtor)
            return null;

        var namingAttr = symbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BindingConstants.JSNamingConventionAttribute);

        var convention = new NamingConvention();
        if (namingAttr is not null)
        {
            var constructors = (NamingPreference)(
                namingAttr
                    .NamedArguments.FirstOrDefault(na =>
                        na.Key == BindingConstants.JSNamingConventionParams.Constructors
                    )
                    .Value.Value
                ?? (int)NamingPreference.Original
            );
            var globals = (NamingPreference)(
                namingAttr
                    .NamedArguments.FirstOrDefault(na => na.Key == BindingConstants.JSNamingConventionParams.Globals)
                    .Value.Value
                ?? (int)NamingPreference.CamelCase
            );
            var enums = (NamingPreference)(
                namingAttr
                    .NamedArguments.FirstOrDefault(na =>
                        na.Key == BindingConstants.JSNamingConventionParams.EnumMembers
                    )
                    .Value.Value
                ?? (int)NamingPreference.Original
            );
            var members = (NamingPreference)(
                namingAttr
                    .NamedArguments.FirstOrDefault(na => na.Key == BindingConstants.JSNamingConventionParams.Members)
                    .Value.Value
                ?? (int)NamingPreference.CamelCase
            );
            var constants = (NamingPreference)(
                namingAttr
                    .NamedArguments.FirstOrDefault(na => na.Key == BindingConstants.JSNamingConventionParams.Constants)
                    .Value.Value
                ?? (int)NamingPreference.ScreamingSnakeCase
            );

            convention = new NamingConvention(constructors, globals, enums, members, constants);
        }

        return new Registry(
            DotnetType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ClassName: symbol.Name,
            Namespace: symbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : symbol.ContainingNamespace.ToDisplayString(),
            NamingConvention: convention
        );
    }
}
