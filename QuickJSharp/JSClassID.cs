using System.Runtime.InteropServices;

namespace QuickJSharp;

/// <summary>
/// Represents a unique identifier for a Javascript class within a <see cref="JSRuntime"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct JSClassID(QuickJSharp.Native.QuickJS.JSClassID NativeValue)
{
    /// <summary>
    /// Gets the underlying raw value of the class ID.
    /// </summary>
    public uint Value => NativeValue.Value;

    /// <summary>
    /// Returns a string representation of the class ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
