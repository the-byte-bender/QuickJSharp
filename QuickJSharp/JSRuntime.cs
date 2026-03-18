using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A QuickJS runtime.
/// </summary>
public sealed unsafe class JSRuntime : IDisposable
{
    /// <summary>
    /// Function signature for the call method of a custom Javascript class.
    /// </summary>
    /// <param name="ctx">The execution context.</param>
    /// <param name="funcObj">The function object being called (borrowed).</param>
    /// <param name="thisVal">The 'this' value (borrowed).</param>
    /// <param name="args">A span of arguments (borrowed).</param>
    /// <param name="flags">Call flags (e.g. <see cref="JSCallFlags.Constructor"/>).</param>
    /// <returns>A <see cref="JSValue"/> that the engine will take ownership of.</returns>
    public delegate JSValue JSClassCall(JSContext ctx, JSValue funcObj, JSValue thisVal, ReadOnlySpan<JSValue> args, JSCallFlags flags);

    /// <summary>
    /// The garbage collection marking callback for a Javascript class instance.
    /// </summary>
    /// <param name="runtime">The runtime where the object resides.</param>
    /// <param name="val">The Javascript object being marked.</param>
    /// <param name="markFuncPtr">The opaque pointer to the QuickJS mark function. Use <see cref="GCMark"/> with this pointer.</param>
    /// <remarks>
    /// This is called during the GC cycle to find reachable Javascript objects held within this instance.
    /// Use <see cref="GCMark"/> for every <see cref="JSValue"/> your object holds.
    /// </remarks>
    public delegate void JSClassGCMarkDelegate(JSRuntime runtime, JSValue val, IntPtr markFuncPtr);

    /// <summary>
    /// The finalizer callback for a Javascript class instance.
    /// </summary>
    /// <param name="runtime">The runtime where the object resides.</param>
    /// <param name="val">The Javascript object being finalized.</param>
    /// <remarks>
    public delegate void JSClassFinalizerDelegate(JSRuntime runtime, JSValue val);

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

    internal record struct JSClassMetadata(
        JSClassFinalizerDelegate? Finalizer,
        JSClassGCMarkDelegate? GCMark,
        JSClassCall? Call
    );

    private QuickJS.JSRuntime* _rt;
    private GCHandle _handle;
    private JSClassMetadata[] _classMetadata = new JSClassMetadata[64];
    private readonly List<IRuntimeExtension> _extensions = [];
    private ModuleNormalizeDelegate? _moduleNormalizer;
    private ModuleLoaderDelegate? _moduleLoader;

    public JSRuntime() : this(QuickJS.JS_NewRuntime())
    {
        if (_rt == null) throw new InvalidOperationException("Failed to create QuickJS runtime.");
    }

    internal JSRuntime(QuickJS.JSRuntime* rt)
    {
        _rt = rt;
        if (_rt != null)
        {
            _handle = GCHandle.Alloc(this);
            QuickJS.JS_SetRuntimeOpaque(_rt, (void*)GCHandle.ToIntPtr(_handle));
        }
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


    /// <summary>
    /// Gets or sets the module normalizer for this runtime.
    /// </summary>
    public ModuleNormalizeDelegate? ModuleNormalizer
    {
        get => _moduleNormalizer;
        set
        {
            _moduleNormalizer = value;
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

    public QuickJS.JSRuntime* NativeRuntime => _rt;

    /// <summary>
    /// An optional property that can be used to associate arbitrary .NET state with this runtime. This is not used by the library.
    /// </summary>
    public Object? Userdata { get; set; }

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

    /// <summary>
    /// Adds an extension to this runtime and initializes it.
    /// </summary>
    /// <param name="extension">The extension to add.</param>
    public void AddExtension(IRuntimeExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        extension.Initialize(this);
        _extensions.Add(extension);
    }

    /// <summary>
    /// Creates a new Javascript context for this runtime with all intrinsics.
    /// </summary>
    /// <returns>A new <see cref="JSContext"/> instance.</returns>
    public JSContext CreateContext()
    {
        QuickJS.JSContext* ctx = QuickJS.JS_NewContext(_rt);
        if (ctx == null) throw new InvalidOperationException("Failed to create QuickJS context.");

        var jsCtx = new JSContext(this, ctx);
        try
        {
            foreach (var ext in _extensions)
            {
                ext.SetupContext(jsCtx);
            }
        }
        catch
        {
            jsCtx.Dispose();
            throw;
        }
        return jsCtx;
    }

    /// <summary>
    /// Creates a new raw Javascript context for this runtime without any standard intrinsics.
    /// </summary>
    /// <remarks>
    /// A raw context contains no standard Javascript globals (no Object, Array, etc.).
    /// Standard features must be added manually using the <c>AddIntrinsic...</c> methods in <see cref="JSContext"/>.
    /// </remarks>
    /// <returns>A new raw <see cref="JSContext"/> instance.</returns>
    public JSContext CreateRawContext()
    {
        QuickJS.JSContext* ctx = QuickJS.JS_NewContextRaw(_rt);
        if (ctx == null) throw new InvalidOperationException("Failed to create raw QuickJS context.");

        var jsCtx = new JSContext(this, ctx);
        try
        {
            foreach (var ext in _extensions)
            {
                ext.SetupContext(jsCtx);
            }
        }
        catch
        {
            jsCtx.Dispose();
            throw;
        }
        return jsCtx;
    }

    /// <summary>
    /// Allocates a new class ID for this runtime.
    /// </summary>
    /// <returns>The new class ID.</returns>
    /// <remarks>
    /// This should be called once per unique object type you wish to register.
    /// The returned ID is used with <see cref="DefineClass"/> and <see cref="JSContext.NewObjectClass"/>.
    /// </remarks>
    public JSClassID NewClassID()
    {
        QuickJS.JSClassID id;
        QuickJS.JS_NewClassID(_rt, &id);
        return new JSClassID(id);
    }

    /// <summary>
    /// Defines a new class for this runtime using C# delegates.
    /// </summary>
    /// <param name="classId">The class ID obtained via <see cref="NewClassID"/>.</param>
    /// <param name="name">The name of the class (must only be pure ASCII characters).</param>
    /// <param name="finalizer">Optional finalizer called when a class instance is garbage collected. Receive the runtime and the object being finalized.</param>
    /// <param name="gcMark">Optional callback called by the garbage collector to find reachable objects within a class instance.</param>
    /// <param name="call">Optional delegate called if the class instance is invoked as a function.</param>
    /// <remarks>
    /// This method has a very slight overhead over the native version. To squeeze just that bit more performence, consider using <see cref="DefineClassNative"/> with unmanaged function pointers.
    /// </remarks>
    public void DefineClass(JSClassID classId, string name,
        JSClassFinalizerDelegate? finalizer = null,
        JSClassGCMarkDelegate? gcMark = null,
        JSClassCall? call = null)
    {
        if (classId.Value >= (uint)_classMetadata.Length)
        {
            Array.Resize(ref _classMetadata, Math.Max((int)classId.Value + 1, _classMetadata.Length * 2));
        }
        _classMetadata[classId.Value] = new JSClassMetadata(finalizer, gcMark, call);

        int nameLen = JSUtils.GetMaxByteCount(name.Length);
        byte* pName = stackalloc byte[nameLen];
        JSUtils.GetUtf8(name, pName, nameLen);

        var def = new QuickJS.JSClassDef
        {
            class_name = pName,
            finalizer = finalizer != null ? &NativeFinalizer : null,
            gc_mark = gcMark != null ? &NativeGCMark : null,
            call = call != null ? &NativeCall : null
        };

        if (QuickJS.JS_NewClass(_rt, classId.NativeValue, &def) < 0)
        {
            throw new InvalidOperationException($"Failed to define class '{name}'.");
        }
    }

    /// <summary>
    /// Defines a new class for this runtime using native unmanaged function pointers.
    /// </summary>
    /// <param name="classId">The class ID obtained via <see cref="NewClassID"/>.</param>
    /// <param name="name">The name of the class (must only be pure ASCII characters).</param>
    /// <param name="finalizer">The native finalizer for class instances.</param>
    /// <param name="gcMark">The native GC mark function for class instances.</param>
    /// <param name="call">The native call function for class instances.</param>
    /// <remarks>
    /// This provides the lowest possible overhead. The provided pointers must be 
    /// compatible with the <c>Cdecl</c> calling convention 
    /// </remarks>
    public void DefineClassNative(JSClassID classId, string name,
        delegate* unmanaged[Cdecl]<QuickJS.JSRuntime*, QuickJS.JSValue, void> finalizer = null,
        delegate* unmanaged[Cdecl]<QuickJS.JSRuntime*, QuickJS.JSValue, delegate* unmanaged[Cdecl]<QuickJS.JSRuntime*, QuickJS.JSGCObjectHeader*, void>, void> gcMark = null,
        delegate* unmanaged[Cdecl]<QuickJS.JSContext*, QuickJS.JSValue, QuickJS.JSValue, int, QuickJS.JSValue*, int, QuickJS.JSValue> call = null)
    {
        int nameLen = JSUtils.GetMaxByteCount(name.Length);
        byte* pName = stackalloc byte[nameLen];
        JSUtils.GetUtf8(name, pName, nameLen);

        var def = new QuickJS.JSClassDef
        {
            class_name = pName,
            finalizer = finalizer,
            gc_mark = gcMark,
            call = call
        };

        if (QuickJS.JS_NewClass(_rt, classId.NativeValue, &def) < 0)
        {
            throw new InvalidOperationException($"Failed to define native class '{name}'.");
        }
    }

    /// <summary>
    /// Marks a Javascript value as reachable during a garbage collection cycle.
    /// </summary>
    /// <param name="markFuncPtr">The mark function pointer received in the <c>GCMark</c> callback.</param>
    /// <param name="val">The Javascript value to mark.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GCMark(IntPtr markFuncPtr, QuickJS.JSValue val)
    {
        QuickJS.JS_MarkValue(_rt, val, (delegate* unmanaged[Cdecl]<QuickJS.JSRuntime*, QuickJS.JSGCObjectHeader*, void>)markFuncPtr);
    }

    private void UpdateModuleLoader()
    {
        if (ModuleNormalizer == null && ModuleLoader == null)
        {
            QuickJS.JS_SetModuleLoaderFunc(_rt, null, null, null);
        }
        else
        {
            QuickJS.JS_SetModuleLoaderFunc(_rt,
                ModuleNormalizer != null ? &NativeNormalize : null,
                ModuleLoader != null ? &NativeLoader : null,
                (void*)GCHandle.ToIntPtr(_handle));
        }
    }

    public void Dispose()
    {
        if (_rt != null)
        {
            foreach (var extension in _extensions)
            {
                try
                {
                    extension.Dispose();
                }
                catch
                {
                    // Swallow exceptions to ensure all extensions get a chance to clean up.
                }
            }
            _extensions.Clear();

            QuickJS.JS_FreeRuntime(_rt);
            _rt = null;
        }
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
        GC.SuppressFinalize(this);
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFinalizer(QuickJS.JSRuntime* rt, QuickJS.JSValue val)
    {
        var runtime = FromNative(rt);
        uint classId = QuickJS.JS_GetClassID(val);

        if (classId < (uint)runtime._classMetadata.Length)
        {
            try
            {
                runtime._classMetadata[classId].Finalizer?.Invoke(runtime, new JSValue(val));
            }
            catch
            {
                // Native finalizers MUST NOT throw exceptions.
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeGCMark(QuickJS.JSRuntime* rt, QuickJS.JSValue val, delegate* unmanaged[Cdecl]<QuickJS.JSRuntime*, QuickJS.JSGCObjectHeader*, void> mark_func)
    {
        var runtime = FromNative(rt);
        uint classId = QuickJS.JS_GetClassID(val);

        if (classId < (uint)runtime._classMetadata.Length)
        {
            try
            {
                runtime._classMetadata[classId].GCMark?.Invoke(runtime, new JSValue(val), (IntPtr)mark_func);
            }
            catch
            {
                // Native GC mark function MUST NOT throw exceptions.
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static QuickJS.JSValue NativeCall(QuickJS.JSContext* ctx, QuickJS.JSValue func_obj, QuickJS.JSValue this_val, int argc, QuickJS.JSValue* argv, int flags)
    {
        var jsContext = JSContext.FromNative(ctx);
        var runtime = jsContext.Runtime;
        uint classId = QuickJS.JS_GetClassID(func_obj);

        if (classId < (uint)runtime._classMetadata.Length)
        {
            try
            {
                var call = runtime._classMetadata[classId].Call;
                if (call is not null)
                {
                    var wrappedArgs = new ReadOnlySpan<JSValue>(argv, argc);
                    return call(jsContext, new JSValue(func_obj), new JSValue(this_val), wrappedArgs, (JSCallFlags)flags).NativeValue;
                }
            }
            catch (Exception ex)
            {
                jsContext.ThrowError(ex.Message);
                return QuickJS.JS_EXCEPTION;
            }
        }
        return QuickJS.JS_EXCEPTION;
    }

    ~JSRuntime() => Dispose();
}

