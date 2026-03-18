using System.Runtime.InteropServices;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A QuickJS atom, representing a unique string or symbol identifier.
/// </summary>
/// <remarks>
/// getting / setting properties with atoms is significantly faster than using strings, especially for repeated access. It's recommended to create atoms for frequently accessed property keys or symbols and reuse them throughout runtime.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe record struct JSAtom(QuickJS.JSAtom NativeValue)
{
    public static readonly JSAtom Null = new(new QuickJS.JSAtom { Value = QuickJS.JS_ATOM_NULL });

    /// <summary>
    /// Gets the raw numerical value of the atom.
    /// </summary>
    public uint Value => NativeValue.Value;

    /// <summary>
    /// Converts the atom to a <see cref="JSValue"/>.
    /// </summary>
    /// <param name="ctx">The context to use for conversion.</param>
    /// <returns>A <see cref="JSValue"/> representing the atom (string or symbol).</returns>
    public JSValue ToValue(JSContext ctx) => new(QuickJS.JS_AtomToValue(ctx.NativeContext, NativeValue));

    /// <summary>
    /// Converts the atom to its string representation.
    /// </summary>
    /// <param name="ctx">The context to use for conversion.</param>
    /// <returns>The string representation of the atom.</returns>
    public string ToString(JSContext ctx)
    {
        nuint len;
        byte* ptr = QuickJS.JS_AtomToCStringLen(ctx.NativeContext, &len, NativeValue);
        if (ptr == null)
            return null!;
        try
        {
            return JSUtils.GetString(ptr, (int)len);
        }
        finally
        {
            QuickJS.js_free(ctx.NativeContext, ptr);
        }
    }

    /// <summary>
    /// Frees the atom through the specified context.
    /// </summary>
    /// <param name="ctx">The context.</param>
    public void Free(JSContext ctx) => QuickJS.JS_FreeAtom(ctx.NativeContext, NativeValue);

    /// <summary>
    /// Frees the atom through the specified runtime.
    /// </summary>
    /// <param name="runtime">The runtime.</param>
    public void Free(JSRuntime runtime) => QuickJS.JS_FreeAtomRT(runtime.NativeRuntime, NativeValue);

    /// <summary>
    /// Creates another independent reference to the same atom, incrementing its reference count.
    /// </summary>
    /// <param name="ctx">The context.</param>
    /// <returns>A new <see cref="JSAtom"/> with an incremented reference count.</returns>
    /// <Remarks>
    /// The returned atom must be independently freed using <see cref="Free(JSContext)"/> or <see cref="Free(JSRuntime)"/>.
    /// </Remarks>
    public JSAtom Duplicate(JSContext ctx) => new(QuickJS.JS_DupAtom(ctx.NativeContext, NativeValue));

    /// <summary>
    /// Duplicates the atom in the specified runtime.
    /// </summary>
    /// <param name="runtime">The runtime.</param>
    /// <returns>A new <see cref="JSAtom"/> with an incremented reference count.</returns>
    /// <Remarks>
    /// The returned atom must be independently freed using <see cref="Free(JSContext)"/> or <see cref="Free(JSRuntime)"/>.
    /// </Remarks>
    public JSAtom Duplicate(JSRuntime runtime) => new(QuickJS.JS_DupAtomRT(runtime.NativeRuntime, NativeValue));

    public static implicit operator JSAtom(QuickJS.JSAtom native) => new(native);

    public static implicit operator QuickJS.JSAtom(JSAtom atom) => atom.NativeValue;
}
