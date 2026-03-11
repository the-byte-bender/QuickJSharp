using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A QuickJS runtime.
/// </summary>
public sealed unsafe class JSRuntime : IDisposable
{
    private QuickJS.JSRuntime* _rt;
    private GCHandle _handle;
    private ModuleNormalizeDelegate? _moduleNormalize;
    private ModuleLoaderDelegate? _moduleLoader;

    /// <summary>
    /// Normalizes a module name.
    /// </summary>
    /// <param name="ctx">The context in which the module is being loaded.</param>
    /// <param name="baseName">The name of the module that is importing this module.</param>
    /// <param name="name">The name of the module to normalize.</param>
    /// <returns>The normalized name, or <c>null</c> if an exception occurred.</returns>
    /// <remarks>
    /// This is called for every <c>import</c> statement to turn the requested string into a canonical ID.
    /// If <c>null</c>, QuickJS uses a default path-based one that handles relative paths.
    /// </remarks>
    public delegate string? ModuleNormalizeDelegate(JSContext ctx, string baseName, string name);

    /// <summary>
    /// Loads a module by name.
    /// </summary>
    /// <param name="ctx">The context in which the module is being loaded.</param>
    /// <param name="name">The normalized name of the module to load.</param>
    /// <returns>The module, or <c>null</c> if it could not be loaded.</returns>
    /// <remarks>
    /// This is called only if the module is not already in the context's cache.
    /// </remarks>
    public delegate JSModule? ModuleLoaderDelegate(JSContext ctx, string name);

    public JSRuntime() : this(QuickJS.JS_NewRuntime())
    {
        if (_rt == null) throw new InvalidOperationException("Failed to create QuickJS runtime.");
    }

    internal JSRuntime(QuickJS.JSRuntime* rt)
    {
        _rt = rt;
        if (_rt != null)
        {
            // Use a strong handle so the C# wrapper stays alive as long as the native runtime needs it
            _handle = GCHandle.Alloc(this);
            QuickJS.JS_SetRuntimeOpaque(_rt, (void*)GCHandle.ToIntPtr(_handle));
        }
    }

    /// <summary>
    /// Gets or sets the module normalizer for this runtime.
    /// </summary>
    public ModuleNormalizeDelegate? ModuleNormalizer
    {
        get => _moduleNormalize;
        set
        {
            _moduleNormalize = value;
            UpdateModuleLoader();
        }
    }

    /// <summary>
    /// Gets or sets the module loader for this runtime.
    /// </summary>
    public ModuleLoaderDelegate? ModuleLoader
    {
        get => _moduleLoader;
        set
        {
            _moduleLoader = value;
            UpdateModuleLoader();
        }
    }

    private void UpdateModuleLoader()
    {
        if (_moduleNormalize == null && _moduleLoader == null)
        {
            QuickJS.JS_SetModuleLoaderFunc(_rt, null, null, null);
        }
        else
        {
            QuickJS.JS_SetModuleLoaderFunc(_rt,
                _moduleNormalize != null ? &NativeNormalize : null,
                _moduleLoader != null ? &NativeLoader : null,
                (void*)GCHandle.ToIntPtr(_handle));
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* NativeNormalize(QuickJS.JSContext* ctx, byte* base_name, byte* name, void* opaque)
    {
        var jsContext = JSContext.FromNative(ctx);
        var runtime = FromNative(QuickJS.JS_GetRuntime(ctx));
        if (runtime.ModuleNormalizer == null) return null;

        try
        {
            string? res = runtime.ModuleNormalizer(
                jsContext,
                JSUtils.GetString(base_name),
                JSUtils.GetString(name));

            if (res == null) return null;

            int len = JSUtils.GetMaxByteCount(res.Length);
            byte* buffer = (byte*)QuickJS.js_malloc(ctx, (nuint)len);
            if (buffer == null) return null;

            JSUtils.GetUtf8(res, buffer, len);
            return buffer;
        }
        catch (Exception ex)
        {
            jsContext.ThrowError(ex.Message);
            return null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static QuickJS.JSModuleDef* NativeLoader(QuickJS.JSContext* ctx, byte* name, void* opaque)
    {
        var jsContext = JSContext.FromNative(ctx);
        var runtime = FromNative(QuickJS.JS_GetRuntime(ctx));
        if (runtime.ModuleLoader == null) return null;

        try
        {
            var module = runtime.ModuleLoader(jsContext, JSUtils.GetString(name));
            return module.HasValue ? module.Value.NativeModule : null;
        }
        catch (Exception ex)
        {
            jsContext.ThrowError(ex.Message);
            return null;
        }
    }

    public QuickJS.JSRuntime* NativeRuntime => _rt;

    /// <summary>
    /// Gets or sets the memory limit for the runtime.
    /// </summary>
    public nuint MemoryLimit
    {
        get
        {
            QuickJS.JSMemoryUsage usage;
            QuickJS.JS_ComputeMemoryUsage(_rt, &usage);
            return (nuint)usage.malloc_limit;
        }
        set => QuickJS.JS_SetMemoryLimit(_rt, value);
    }

    /// <summary>
    /// Gets or sets the maximum stack size for the runtime.
    /// </summary>
    public nuint MaxStackSize
    {
        set => QuickJS.JS_SetMaxStackSize(_rt, value);
    }

    /// <summary>
    /// Update the stack top for stack overflow detection.
    /// </summary>
    public void UpdateStackTop() => QuickJS.JS_UpdateStackTop(_rt);

    /// <summary>
    /// Run the garbage collector.
    /// </summary>
    public void RunGC() => QuickJS.JS_RunGC(_rt);

    /// <summary>
    /// Check if there are pending jobs (promises) to execute.
    /// </summary>
    public bool IsJobPending => QuickJS.JS_IsJobPending(_rt);

    /// <summary>
    /// Execute one pending job (e.g. promise resolution).
    /// </summary>
    /// <param name="ctx">The context the job ran in, if any.</param>
    /// <returns>1 if a job was executed, 0 if no job was pending, -1 if an exception occurred.</returns>
    public int ExecutePendingJob(out JSContext? ctx)
    {
        QuickJS.JSContext* pNativeCtx;
        int res = QuickJS.JS_ExecutePendingJob(_rt, &pNativeCtx);
        ctx = pNativeCtx != null ? JSContext.FromNative(pNativeCtx) : null;
        return res;
    }

    /// <summary>
    /// Gets the memory usage statistics.
    /// </summary>
    public QuickJS.JSMemoryUsage GetMemoryUsage()
    {
        QuickJS.JSMemoryUsage usage;
        QuickJS.JS_ComputeMemoryUsage(_rt, &usage);
        return usage;
    }

    public JSContext CreateContext()
    {
        var ctx = QuickJS.JS_NewContext(_rt);
        if (ctx == null) throw new InvalidOperationException("Failed to create QuickJS context.");
        return new JSContext(this, ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSRuntime FromNative(QuickJS.JSRuntime* rt)
    {
        if (rt == null) throw new ArgumentNullException(nameof(rt));

        IntPtr ptr = (IntPtr)QuickJS.JS_GetRuntimeOpaque(rt);
        if (ptr != IntPtr.Zero)
        {
            var h = GCHandle.FromIntPtr(ptr);
            if (h.Target is JSRuntime existing) return existing;
        }

        return new JSRuntime(rt);
    }

    public void Dispose()
    {
        if (_rt != null)
        {
            QuickJS.JS_FreeRuntime(_rt);
            _rt = null;
        }
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
        GC.SuppressFinalize(this);
    }

    ~JSRuntime() => Dispose();
}

