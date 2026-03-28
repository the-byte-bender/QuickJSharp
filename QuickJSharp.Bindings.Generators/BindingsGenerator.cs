using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QuickJSharp.Bindings.Generators.Emitters;
using QuickJSharp.Bindings.Generators.Extraction;

namespace QuickJSharp.Bindings.Generators;

[Generator]
public class BindingsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var exposedTypes = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                BindingConstants.JSExposeAttribute,
                static (n, _) =>
                    n switch
                    {
                        ClassDeclarationSyntax => true,
                        InterfaceDeclarationSyntax => true,
                        EnumDeclarationSyntax => true,
                        RecordDeclarationSyntax r => r.Modifiers.All(m => !m.IsKind(SyntaxKind.RefKeyword)),
                        StructDeclarationSyntax s => s.Modifiers.All(m => !m.IsKind(SyntaxKind.RefKeyword)),
                        _ => false,
                    },
                ExposedTypeExtractor.Transform
            )
            .Where(static t => t is not null)
            .Select(static (t, _) => t!.Value);

        var registries = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                BindingConstants.JSBindingsRegistryAttribute,
                static (n, _) =>
                    n is ClassDeclarationSyntax classSyntax
                    && classSyntax.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword))
                    && classSyntax.Modifiers.All(mod => !mod.IsKind(SyntaxKind.StaticKeyword)),
                static (ctx, ct) => RegistryExtractor.Transform(ctx, ct)
            )
            .Where(static r => r is not null)
            .Select(static (r, _) => r!.Value);

        context.RegisterSourceOutput(
            registries,
            static (ctx, registry) =>
            {
                var source = RegistryEmitter.Emit(registry);
                ctx.AddSource($"{registry.ClassName}.g.cs", source);
            }
        );

        var registriesCollected = registries.Collect();

        var registrationPairs = exposedTypes
            .Combine(registriesCollected)
            .SelectMany(
                static (tuple, _) =>
                {
                    var (exposedType, registryArray) = tuple;
                    return registryArray
                        .Where(r =>
                            exposedType.ExposedFor is null
                            || exposedType.ExposedFor.Value.IsEmpty
                            || exposedType.ExposedFor.Value.GetInner().Contains(r.DotnetType)
                        )
                        .Select(r => (exposedType: exposedType.ResolveAgainstRegistry(r), registry: r));
                }
            );

        context.RegisterSourceOutput(
            registrationPairs,
            static (ctx, pair) =>
            {
                var (exposedType, registry) = pair;
                var source = BindingEmitter.Emit(exposedType, registry);
                ctx.AddSource($"{registry.ClassName}/{exposedType.DotnetType.Split('.').Last()}.g.cs", source);
            }
        );
    }
}
