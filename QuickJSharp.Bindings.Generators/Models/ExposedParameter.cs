namespace QuickJSharp.Bindings.Generators.Models;

/// <summary>
/// Specifies the passing modifier of a parameter.
/// </summary>
enum ParameterModifier
{
    None,
    Ref,
    Out,
    In,
}

/// <summary>
/// Represents a parameter to an exposed .NET member.
/// </summary>
/// <param name="ParameterType">The type information for the parameter.</param>
/// <param name="DotnetName">The name of the parameter in .NET code.</param>
/// <param name="JSName">
/// The name of the parameter as it will appear in JavaScript.
/// Guaranteed to be not null in the final model after processing against context.
/// </param>
/// <param name="Modifier">The passing modifier of the parameter.</param>
/// <param name="IsParams">Whether the parameter is a <c>params</c> array.</param>
/// <param name="HasDefaultValue">Whether the parameter has a default value in .NET.</param>
readonly record struct ExposedParameter(
    TypeInfo ParameterType,
    string DotnetName,
    string? JSName = null,
    ParameterModifier Modifier = ParameterModifier.None,
    bool IsParams = false,
    bool HasDefaultValue = false
);
