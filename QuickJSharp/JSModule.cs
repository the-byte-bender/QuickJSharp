using System.Buffers;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A QuickJS module definition.
/// </summary>
/// <remarks>
/// This is a transient view of a module definition, used during initialization 
/// or loading. It does not own the underlying native memory.
/// </remarks>
public readonly unsafe struct JSModule
{
    private readonly QuickJS.JSModuleDef* _m;
    private readonly JSContext _ctx;

    internal JSModule(JSContext ctx, QuickJS.JSModuleDef* m)
    {
        _ctx = ctx;
        _m = m;
    }

    public QuickJS.JSModuleDef* NativeModule => _m;

    /// <summary>
    /// Adds an export to this module definition.
    /// </summary>
    /// <remarks>
    /// This MUST be called during the Loader phase (within <see cref="JSRuntime.ModuleLoader"/>)
    /// BEFORE the module is returned to the engine. This defines the metadata/shape of the module.
    /// </remarks>
    /// <param name="name">The name of the export.</param>
    /// <returns>0 on success, &lt; 0 on failure.</returns>
    public int AddExport(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            return QuickJS.JS_AddModuleExport(_ctx.NativeContext, _m, pName);
        }

        byte[] array = ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                return QuickJS.JS_AddModuleExport(_ctx.NativeContext, _m, pName);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Sets the value of a previously added export.
    /// </summary>
    /// <remarks>
    /// This MUST be called within the Module Initialization callback. 
    /// Calling this at any other time will likely fail or cause undefined behavior.
    /// </remarks>
    /// <param name="name">The name of the export (must have been added via AddExport).</param>
    /// <param name="val">The value to assign. The module definition takes ownership of this reference; do not call Dispose/Free on the value again.</param>
    /// <returns>0 on success, &lt; 0 on failure.</returns>
    public int SetExport(string name, JSValue val)
    {
        ArgumentNullException.ThrowIfNull(name);

        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            return QuickJS.JS_SetModuleExport(_ctx.NativeContext, _m, pName, val.NativeValue);
        }

        byte[] array = ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                return QuickJS.JS_SetModuleExport(_ctx.NativeContext, _m, pName, val.NativeValue);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
