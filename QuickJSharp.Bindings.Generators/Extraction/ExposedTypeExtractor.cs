using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using QuickJSharp.Bindings.Generators.Infrastructure;
using QuickJSharp.Bindings.Generators.Models;

namespace QuickJSharp.Bindings.Generators.Extraction;

static class ExposedTypeExtractor
{
    public static ExposedType? Transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var attributeData = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == BindingConstants.JSExposeAttribute
        );

        if (attributeData is null)
            return null;

        var exposedFor = GetExposedFor(attributeData);
        var (jsName, overridenPref) = GetJSNaming(symbol);

        var members = new List<ExposedMember>();

        var exposeImplicit =
            attributeData
                .NamedArguments.FirstOrDefault(kvp =>
                    kvp.Key == BindingConstants.JSExposeParams.ExposeImplicitConstructor
                )
                .Value.Value
            is true;

        foreach (var memberSymbol in symbol.GetMembers())
        {
            var memberAttr = memberSymbol
                .GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BindingConstants.JSExposeAttribute);

            var isImplicitEnum = IsImplicitlyExposedEnumMember(symbol, memberSymbol);
            if (memberAttr is not null || isImplicitEnum)
            {
                members.Add(TransformMember(memberSymbol, memberAttr, isImplicitEnum));
            }
        }

        return new ExposedType(
            DotnetType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DotnetName: symbol.Name,
            Kind: GetTypeKind(symbol),
            Members: new EquatableArray<ExposedMember>(members.ToImmutableArray()),
            BaseType: symbol.BaseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ExposeImplicitConstructor: exposeImplicit,
            IsStatic: symbol.IsStatic,
            ExposedFor: exposedFor == null ? null : new EquatableArray<string>(exposedFor.Value.ToImmutableArray()),
            JSName: jsName,
            OverriddenNamingPreference: overridenPref
        );
    }

    private static bool IsImplicitlyExposedEnumMember(INamedTypeSymbol parent, ISymbol member) =>
        parent.TypeKind == TypeKind.Enum && member is IFieldSymbol field && field.ConstantValue != null;

    private static ExposedMember TransformMember(ISymbol symbol, AttributeData? attr, bool isImplicitEnum)
    {
        var exposedFor = attr is not null ? GetExposedFor(attr) : null;
        var (jsName, overridenPref) = GetJSNaming(symbol);
        var kind = GetMemberKind(symbol);

        var parameters = ImmutableArray<ExposedParameter>.Empty;
        Models.TypeInfo? returnType = null;
        bool isRefReturn = false;
        bool hasGetter = false;
        string? getterName = null;
        bool hasSetter = false;
        string? setterName = null;
        bool isReadOnly = false;

        if (symbol is IMethodSymbol method)
        {
            parameters = method.Parameters.Select(TransformParameter).ToImmutableArray();
            returnType = TransformType(method.ReturnType);
            isRefReturn = method.ReturnsByRef || method.ReturnsByRefReadonly;
        }
        else if (symbol is IPropertySymbol prop)
        {
            returnType = TransformType(prop.Type);
            hasGetter =
                prop.GetMethod != null
                && prop.GetMethod.DeclaredAccessibility == Accessibility.Public
                && ShouldExposeAccessor(attr, BindingConstants.JSExposeParams.Read);
            getterName = prop.GetMethod?.Name;
            hasSetter =
                prop.SetMethod != null
                && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
                && ShouldExposeAccessor(attr, BindingConstants.JSExposeParams.Write);
            setterName = prop.SetMethod?.Name;
            isReadOnly = hasGetter && !hasSetter;
            isRefReturn = prop.ReturnsByRef || prop.ReturnsByRefReadonly;
        }
        else if (symbol is IFieldSymbol field)
        {
            returnType = TransformType(field.Type);
            isReadOnly = field.IsReadOnly || field.IsConst || isImplicitEnum;
            hasGetter =
                isImplicitEnum
                || (
                    field.DeclaredAccessibility == Accessibility.Public
                    && ShouldExposeAccessor(attr, BindingConstants.JSExposeParams.Read)
                );
            hasSetter =
                !isReadOnly
                && field.DeclaredAccessibility == Accessibility.Public
                && ShouldExposeAccessor(attr, BindingConstants.JSExposeParams.Write);
        }

        return new ExposedMember(
            DotnetName: symbol.Name,
            Kind: kind,
            IsStatic: symbol.IsStatic,
            IsOverride: symbol.IsOverride,
            JSName: jsName,
            OverriddenNamingPreference: overridenPref,
            Parameters: new EquatableArray<ExposedParameter>(parameters),
            ReturnType: returnType,
            IsRefReturn: isRefReturn,
            HasGetter: hasGetter,
            GetterMethodName: getterName,
            HasSetter: hasSetter,
            SetterMethodName: setterName,
            IsReadOnly: isReadOnly,
            ExposedFor: exposedFor == null ? null : new EquatableArray<string>(exposedFor.Value.ToImmutableArray())
        );
    }

    private static Models.TypeInfo TransformType(ITypeSymbol symbol)
    {
        return new Models.TypeInfo(
            DotnetType: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsEnum: symbol.TypeKind == TypeKind.Enum,
            UnderlyingType: (symbol as INamedTypeSymbol)?.EnumUnderlyingType?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            )
        );
    }

    private static bool ShouldExposeAccessor(AttributeData? attr, string key) =>
        attr is null || attr.NamedArguments.All(na => na.Key != key || (bool)na.Value.Value!);

    private static (string? jsName, NamingPreference? preference) GetJSNaming(ISymbol symbol)
    {
        var jsNameAttr = GetAttribute(symbol, BindingConstants.JSNameAttribute, true);
        if (jsNameAttr is not null)
        {
            if (jsNameAttr.ConstructorArguments.Length > 0)
            {
                var arg = jsNameAttr.ConstructorArguments[0];
                if (arg.Value is string s)
                {
                    return (s, null);
                }
                if (arg.Value is int i)
                {
                    return (null, (NamingPreference)i);
                }
            }
        }

        return (null, null);
    }

    private static AttributeData? GetAttribute(ISymbol symbol, string attributeName, bool inherit = false)
    {
        var attr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName);
        if (attr is not null || !inherit)
            return attr;

        ISymbol? baseSymbol = symbol switch
        {
            IMethodSymbol m => m.OverriddenMethod,
            IPropertySymbol p => p.OverriddenProperty,
            IEventSymbol e => e.OverriddenEvent,
            _ => null,
        };

        return baseSymbol is not null ? GetAttribute(baseSymbol, attributeName, inherit) : null;
    }

    private static ExposedParameter TransformParameter(IParameterSymbol symbol)
    {
        var modifier = ParameterModifier.None;
        if (symbol.RefKind == RefKind.Ref)
            modifier = ParameterModifier.Ref;
        else if (symbol.RefKind == RefKind.Out)
            modifier = ParameterModifier.Out;
        else if (symbol.RefKind == RefKind.In)
            modifier = ParameterModifier.In;

        var (jsName, _) = GetJSNaming(symbol);
        return new ExposedParameter(
            ParameterType: TransformType(symbol.Type),
            DotnetName: symbol.Name,
            JSName: jsName,
            Modifier: modifier,
            IsParams: symbol.IsParams,
            HasDefaultValue: symbol.HasExplicitDefaultValue
        );
    }

    private static ImmutableArray<string>? GetExposedFor(AttributeData attr)
    {
        var args = attr.ConstructorArguments;
        if (args.Length == 0)
            return null;

        var values = args[0].Values;
        if (values.IsDefaultOrEmpty)
            return null;

        return values
            .Select(v => ((ITypeSymbol)v.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();
    }

    private static ExposedTypeKind GetTypeKind(INamedTypeSymbol symbol) =>
        symbol.TypeKind switch
        {
            TypeKind.Enum => ExposedTypeKind.Enum,
            TypeKind.Interface => ExposedTypeKind.Interface,
            TypeKind.Struct => ExposedTypeKind.Struct,
            _ => ExposedTypeKind.Class,
        };

    private static ExposedMemberKind GetMemberKind(ISymbol symbol) =>
        symbol switch
        {
            IMethodSymbol m when m.MethodKind == MethodKind.Constructor => ExposedMemberKind.Constructor,
            IMethodSymbol => ExposedMemberKind.Method,
            IPropertySymbol => ExposedMemberKind.Property,
            IFieldSymbol f when f.IsConst => ExposedMemberKind.Const,
            IFieldSymbol => ExposedMemberKind.Field,
            IEventSymbol => ExposedMemberKind.Event,
            _ => throw new System.NotSupportedException($"Symbol kind {symbol.Kind} is not supported."),
        };
}
