namespace QuickJSharp.Bindings.Generators.Models;

readonly record struct Registry(
    // The full .NET type name of the registry, with global:: prefix. This is the source of truth for the .NET type in
    // code generation.
    string DotnetType,
    string ClassName,
    string? Namespace,
    NamingConvention NamingConvention = new()
);
