using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A wrapper around a QuickJS context.
/// </summary>
public sealed unsafe class JSContext : IDisposable
{
    /// <summary>
    /// Function signature for a standard Javascript function.
    /// </summary>
    /// <param name="ctx">The execution context.</param>
    /// <param name="thisVal">The 'this' value (borrowed).</param>
    /// <param name="args">A span of arguments (borrowed).</param>
    /// <returns>A <see cref="JSValue"/> that the engine will take ownership of.</returns>
    public delegate JSValue JSFunction(JSContext ctx, JSValue thisVal, ReadOnlySpan<JSValue> args);

    public delegate int JSModuleInitDelegate(JSContext ctx, JSModule m);

    private QuickJS.JSContext* _ctx;
    private readonly JSRuntime _rt;
    private GCHandle _handle;
    private Dictionary<IntPtr, JSModuleInitDelegate>? _moduleInits;

    internal JSContext(JSRuntime rt, QuickJS.JSContext* ctx)
    {
        _rt = rt;
        _ctx = ctx;

        if (_ctx != null)
        {
            // Use a strong handle so the C# wrapper stays alive as long as the native context needs it
            _handle = GCHandle.Alloc(this);
            QuickJS.JS_SetContextOpaque(_ctx, (void*)GCHandle.ToIntPtr(_handle));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSContext FromNative(QuickJS.JSContext* ctx)
    {
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        IntPtr ptr = (IntPtr)QuickJS.JS_GetContextOpaque(ctx);
        if (ptr != IntPtr.Zero)
        {
            var h = GCHandle.FromIntPtr(ptr);
            if (h.Target is JSContext existing) return existing;
        }

        var rt = JSRuntime.FromNative(QuickJS.JS_GetRuntime(ctx));
        return new JSContext(rt, ctx);
    }

    public QuickJS.JSContext* NativeContext => _ctx;
    public JSRuntime Runtime => _rt;

    /// <summary>
    /// An optional property that can be used to associate arbitrary .NET state with this context. This is not used by the library.
    /// </summary>
    public Object? Userdata { get; set; }

    public JSValue Null => JSValue.Null;
    public JSValue Undefined => JSValue.Undefined;
    public JSValue True => JSValue.True;
    public JSValue False => JSValue.False;
    public JSValue Uninitialized => JSValue.Uninitialized;

    /// <summary>
    /// The global object of this context.
    /// </summary>
    public JSValue GlobalObject => new(QuickJS.JS_GetGlobalObject(_ctx));

    /// <summary>
    /// Check if there is a pending exception.
    /// </summary>
    public bool HasException => QuickJS.JS_HasException(_ctx);

    /// <summary>
    /// Get the pending exception and clear it.
    /// </summary>
    public JSValue GetException() => new(QuickJS.JS_GetException(_ctx));

    /// <summary>
    /// Clear the current exception.
    /// </summary>
    public void ClearException() => QuickJS.JS_FreeValue(_ctx, QuickJS.JS_GetException(_ctx));

    /// <summary>
    /// Sets the current pending exception.
    /// </summary>
    public JSValue Throw(JSValue error) => new(QuickJS.JS_Throw(_ctx, error.NativeValue));

    /// <summary>
    /// Throws a generic Error with the given message.
    /// </summary>
    public JSValue ThrowError(string message)
    {
        if (message is null) return new JSValue(QuickJS.JS_ThrowInternalError(_ctx, null));
        int maxLen = JSUtils.GetMaxByteCount(message.Length);
        if (maxLen <= 512)
        {
            byte* pMsg = stackalloc byte[512];
            JSUtils.GetUtf8(message, pMsg, 512);
            return new JSValue(QuickJS.JS_ThrowInternalError(_ctx, pMsg));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pMsg = array)
            {
                JSUtils.GetUtf8(message, pMsg, maxLen);
                return new JSValue(QuickJS.JS_ThrowInternalError(_ctx, pMsg));
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    /// <summary>
    /// Throws a TypeError with the given message.
    /// </summary>
    public JSValue ThrowTypeError(string message)
    {
        if (message is null) return new JSValue(QuickJS.JS_ThrowTypeError(_ctx, null));
        int maxLen = JSUtils.GetMaxByteCount(message.Length);
        if (maxLen <= 512)
        {
            byte* pMsg = stackalloc byte[512];
            JSUtils.GetUtf8(message, pMsg, 512);
            return new JSValue(QuickJS.JS_ThrowTypeError(_ctx, pMsg));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pMsg = array)
            {
                JSUtils.GetUtf8(message, pMsg, maxLen);
                return new JSValue(QuickJS.JS_ThrowTypeError(_ctx, pMsg));
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    /// <summary>
    /// Retrieves the prototype associated with a specific class ID for this context.
    /// </summary>
    /// <param name="classId">The class ID to query.</param>
    /// <returns>The prototype <see cref="JSValue"/>.</returns>
    public JSValue GetClassProto(JSClassID classId) => new JSValue(QuickJS.JS_GetClassProto(_ctx, classId.NativeValue));

    /// <summary>
    /// Sets the default prototype associated with a specific class ID for this context.
    /// </summary>
    /// <param name="classId">The class ID to set the prototype for.</param>
    /// <param name="proto">The prototype object.</param>
    /// <remarks>
    /// This prototype is used by <see cref="NewObjectClass"/> to initialize the internal 
    /// [[Prototype]] of new instances. It is optional if instances are created exclusively 
    /// using <see cref="NewObjectProtoClass"/> with an explicit prototype.
    /// </remarks>
    public void SetClassProto(JSClassID classId, JSValue proto)
    {
        QuickJS.JS_SetClassProto(_ctx, classId.NativeValue, proto.NativeValue);
    }

    /// <summary>
    /// Creates a new Javascript string.
    /// </summary>
    public JSValue NewString(string value)
    {
        if (value is null) return new JSValue(QuickJS.JS_NewString(_ctx, null));
        int maxLen = JSUtils.GetMaxByteCount(value.Length);
        if (maxLen <= 512)
        {
            byte* pStr = stackalloc byte[512];
            JSUtils.GetUtf8(value, pStr, 512);
            return new JSValue(QuickJS.JS_NewString(_ctx, pStr));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pStr = array)
            {
                JSUtils.GetUtf8(value, pStr, maxLen);
                return new JSValue(QuickJS.JS_NewString(_ctx, pStr));
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    /// <summary>
    /// Creates a new <see cref="JSAtom"/> from a string.
    /// </summary>
    public JSAtom NewAtom(string? value)
    {
        if (value is null) return JSAtom.Null;
        int maxLen = JSUtils.GetMaxByteCount(value.Length);
        if (maxLen <= 512)
        {
            byte* pStr = stackalloc byte[512];
            JSUtils.GetUtf8(value, pStr, 512);
            return QuickJS.JS_NewAtom(_ctx, pStr);
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pStr = array)
            {
                JSUtils.GetUtf8(value, pStr, maxLen);
                return QuickJS.JS_NewAtom(_ctx, pStr);
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    /// <summary>
    /// Creates a new <see cref="JSAtom"/> from a uint32.
    /// </summary>
    public JSAtom NewAtom(uint value) => QuickJS.JS_NewAtomUInt32(_ctx, value);

    /// <summary>
    /// Creates a new unique <see cref="JSAtom"/> representing a Javascript Symbol.
    /// </summary>
    /// <param name="description">An optional description for the symbol.</param>
    /// <returns>A unique <see cref="JSAtom"/>.</returns>
    public JSAtom NewSymbol(string? description = null)
    {
        QuickJS.JSValue symValue;
        if (description != null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(description + "\0");
            fixed (byte* ptr = bytes)
            {
                symValue = QuickJS.JS_NewSymbol(_ctx, ptr, false);
            }
        }
        else
        {
            symValue = QuickJS.JS_NewSymbol(_ctx, null, false);
        }

        try
        {
            return QuickJS.JS_ValueToAtom(_ctx, symValue);
        }
        finally
        {
            QuickJS.JS_FreeValue(_ctx, symValue);
        }
    }

    /// <summary>
    /// Creates a new Javascript Int32.
    /// </summary>
    public JSValue NewInt32(int value) => new(QuickJS.JS_NewInt32(_ctx, value));

    /// <summary>
    /// Creates a new Javascript Int64.
    /// </summary>
    public JSValue NewInt64(long value) => new(QuickJS.JS_NewInt64(_ctx, value));

    /// <summary>
    /// Creates a new Javascript Float64.
    /// </summary>
    public JSValue NewDouble(double value) => new(QuickJS.JS_NewFloat64(_ctx, value));

    /// <summary>
    /// Creates a new Javascript Boolean.
    /// </summary>
    public JSValue NewBoolean(bool value) => value ? JSValue.True : JSValue.False;

    /// <summary>
    /// Creates a new Javascript BigInt from a 64-bit signed integer.
    /// </summary>
    public JSValue NewBigInt64(long value) => new(QuickJS.JS_NewBigInt64(_ctx, value));

    /// <summary>
    /// Creates a new Javascript BigInt from a 64-bit unsigned integer.
    /// </summary>
    public JSValue NewBigUint64(ulong value) => new(QuickJS.JS_NewBigUint64(_ctx, value));

    /// <summary>
    /// Creates a new empty Javascript object.
    /// </summary>
    public JSValue NewObject() => new(QuickJS.JS_NewObject(_ctx));

    /// <summary>
    /// Creates a new object instance of a specific class.
    /// </summary>
    /// <param name="classId">The class ID for the object.</param>
    /// <returns>A new <see cref="JSValue"/> object.</returns>
    /// <remarks>
    /// The object's internal [[Prototype]] is set to the value previously provided to <see cref="SetClassProto"/>. 
    /// If no prototype has been set for this <paramref name="classId"/>, the new object will have a <c>null</c> prototype.
    /// </remarks>
    public JSValue NewObjectClass(JSClassID classId) => new JSValue(QuickJS.JS_NewObjectClass(_ctx, classId.NativeValue));

    /// <summary>
    /// Creates a new object instance of a specific class with an explicit prototype.
    /// </summary>
    /// <param name="proto">The prototype object to use for the new instance.</param>
    /// <param name="classId">The class ID for the object.</param>
    /// <returns>A new <see cref="JSValue"/> object.</returns>
    /// <remarks>
    /// This method allows specifying a prototype directly, bypassing the default registration managed by <see cref="SetClassProto"/> if any.
    /// </remarks>
    public JSValue NewObjectProtoClass(JSValue proto, JSClassID classId) => new JSValue(QuickJS.JS_NewObjectProtoClass(_ctx, proto.NativeValue, classId.NativeValue));

    /// <summary>
    /// Creates a new empty Javascript array.
    /// </summary>
    public JSValue NewArray() => new(QuickJS.JS_NewArray(_ctx));

    /// <summary>
    /// Creates a new Javascript Error object.
    /// </summary>
    public JSValue NewError() => new(QuickJS.JS_NewError(_ctx));

    /// <summary>
    /// Creates a new Javascript Date object.
    /// </summary>
    public JSValue NewDate(double epochMs) => new(QuickJS.JS_NewDate(_ctx, epochMs));

    /// <summary>
    /// Creates a JS function from a delegate.
    /// </summary>
    public JSValue NewFunction(JSFunction func, string name, int length = 0)
    {
        GCHandle handle = GCHandle.Alloc(func);
        void* pOpaque = (void*)GCHandle.ToIntPtr(handle);
        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            return new JSValue(QuickJS.JS_NewCClosure(_ctx, &ManagedFunctionBridge, pName, &ManagedFunctionFinalizer, length, 0, pOpaque));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                return new JSValue(QuickJS.JS_NewCClosure(_ctx, &ManagedFunctionBridge, pName, &ManagedFunctionFinalizer, length, 0, pOpaque));
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Creates a JS function from a raw unmanaged C function pointer.
    /// </summary>
    public JSValue NewFunctionRaw(delegate* unmanaged[Cdecl]<QuickJS.JSContext*, QuickJS.JSValue, int, QuickJS.JSValue*, QuickJS.JSValue> func, string name, int length = 0)
    {
        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            return new JSValue(QuickJS.JS_NewCFunction(_ctx, func, pName, length));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                return new JSValue(QuickJS.JS_NewCFunction(_ctx, func, pName, length));
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Creates a new native module builder for this context.
    /// </summary>
    /// <param name="name">The name of the module.</param>
    /// <returns>A module builder instance.</returns>
    public JSModuleBuilder CreateModule(string name) => new(this, name);

    /// <summary>
    /// Creates a new C module.
    /// </summary>
    /// <param name="name">The name of the module.</param>
    /// <param name="init">The module initialization callback.</param>
    /// <returns>The module, or <c>null</c> if it could not be created.</returns>
    public JSModule? NewModule(string name, JSModuleInitDelegate init)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(init);

        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        QuickJS.JSModuleDef* m;
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            m = QuickJS.JS_NewCModule(_ctx, pName, &NativeModuleInit);
        }
        else
        {
            byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
            try
            {
                fixed (byte* pName = array)
                {
                    JSUtils.GetUtf8(name, pName, maxLen);
                    m = QuickJS.JS_NewCModule(_ctx, pName, &NativeModuleInit);
                }
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
        }

        if (m == null) return null;

        // Map the ModuleDef pointer to its initialization delegate for robust callback routing
        _moduleInits ??= [];
        _moduleInits[(IntPtr)m] = init;

        return new JSModule(this, m);
    }

    /// <summary>
    /// Adds standard Javascript base objects (Object, Function, Number, String, Math, etc.) to this context.
    /// </summary>
    public void AddIntrinsicBaseObjects() => QuickJS.JS_AddIntrinsicBaseObjects(_ctx);

    /// <summary>
    /// Adds the Date object to this context.
    /// </summary>
    public void AddIntrinsicDate() => QuickJS.JS_AddIntrinsicDate(_ctx);

    /// <summary>
    /// Adds support for the <See cref="Eval"/> method, the global eval() function and string-to-code compilation (e.g. via 'new Function()').
    /// </summary>
    public void AddIntrinsicEval() => QuickJS.JS_AddIntrinsicEval(_ctx);

    /// <summary>
    /// Adds support for regular expressions and the RegExp object.
    /// </summary>
    public void AddIntrinsicRegExp()
    {
        QuickJS.JS_AddIntrinsicRegExpCompiler(_ctx);
        QuickJS.JS_AddIntrinsicRegExp(_ctx);
    }

    /// <summary>
    /// Adds the JSON object for parsing and stringifying.
    /// </summary>
    public void AddIntrinsicJSON() => QuickJS.JS_AddIntrinsicJSON(_ctx);

    /// <summary>
    /// Adds support for the Proxy object.
    /// </summary>
    public void AddIntrinsicProxy() => QuickJS.JS_AddIntrinsicProxy(_ctx);

    /// <summary>
    /// Adds Map, Set, WeakMap, and WeakSet collections.
    /// </summary>
    public void AddIntrinsicMapSet() => QuickJS.JS_AddIntrinsicMapSet(_ctx);

    /// <summary>
    /// Adds TypedArrays (Uint8Array, etc.) and ArrayBuffer constructors.
    /// </summary>
    public void AddIntrinsicTypedArrays() => QuickJS.JS_AddIntrinsicTypedArrays(_ctx);

    /// <summary>
    /// Adds Promise support and async/await functionality.
    /// </summary>
    public void AddIntrinsicPromise() => QuickJS.JS_AddIntrinsicPromise(_ctx);

    /// <summary>
    /// Adds support for the BigInt object.
    /// </summary>
    public void AddIntrinsicBigInt() => QuickJS.JS_AddIntrinsicBigInt(_ctx);

    /// <summary>
    /// Adds WeakRef and FinalizationRegistry support.
    /// </summary>
    public void AddIntrinsicWeakRef() => QuickJS.JS_AddIntrinsicWeakRef(_ctx);

    /// <summary>
    /// Adds the performance object (performance.now(), etc.).
    /// </summary>
    public void AddPerformance() => QuickJS.JS_AddPerformance(_ctx);

    /// <summary>
    /// Adds the DOMException error type.
    /// </summary>
    public void AddIntrinsicDOMException() => QuickJS.JS_AddIntrinsicDOMException(_ctx);

    /// <summary>
    /// Evaluates a script.
    /// </summary>
    public JSValue Eval(string script, string filename = "input", JSEvalFlags flags = JSEvalFlags.Global)
    {
        if (script is null) return default;

        int scriptMax = JSUtils.GetMaxByteCount(script.Length);
        int fileMax = JSUtils.GetMaxByteCount(filename.Length);

        byte* pScript = null;
        byte[]? scriptArray = null;
        byte* pFile = null;
        byte[]? fileArray = null;

        try
        {
            if (scriptMax <= 512)
            {
                byte* pStack = stackalloc byte[512];
                pScript = pStack;
            }
            else
            {
                scriptArray = System.Buffers.ArrayPool<byte>.Shared.Rent(scriptMax);
            }

            if (fileMax <= 512)
            {
                byte* pStack = stackalloc byte[512];
                pFile = pStack;
            }
            else
            {
                fileArray = System.Buffers.ArrayPool<byte>.Shared.Rent(fileMax);
            }

            fixed (byte* pS = scriptArray, pF = fileArray)
            {
                byte* sPtr = pScript != null ? pScript : pS;
                byte* fPtr = pFile != null ? pFile : pF;

                int sLen = JSUtils.GetUtf8(script, sPtr, scriptMax);
                JSUtils.GetUtf8(filename, fPtr, fileMax);

                return new JSValue(QuickJS.JS_Eval(_ctx, sPtr, (nuint)sLen, fPtr, (int)flags));
            }
        }
        finally
        {
            if (scriptArray != null) System.Buffers.ArrayPool<byte>.Shared.Return(scriptArray);
            if (fileArray != null) System.Buffers.ArrayPool<byte>.Shared.Return(fileArray);
        }
    }

    /// <summary>
    /// Execute a bytecode object (JSValue with TAG_FUNCTION_BYTECODE).
    /// </summary>
    public JSValue EvalFunction(JSValue bytecode) => new(QuickJS.JS_EvalFunction(_ctx, bytecode.NativeValue));

    /// <summary>
    /// Serialize a JSValue to bytecode.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="flags">Serialization flags.</param>
    /// <returns>A new byte array containing the bytecode.</returns>
    public byte[]? WriteObject(JSValue value, JSWriteObjectFlags flags = JSWriteObjectFlags.Bytecode)
    {
        nuint size;
        byte* ptr = QuickJS.JS_WriteObject(_ctx, &size, value.NativeValue, (int)flags);
        if (ptr == null)
        {
            var ex = GetException();
            if (ex.IsException) return null; // Already an exception
            if (ex.IsNull || ex.IsUndefined) return null;
            throw new JSException("WriteObject failed: " + ex.ToString(this));
        }

        try
        {
            byte[] result = new byte[(int)size];
            new ReadOnlySpan<byte>(ptr, (int)size).CopyTo(result);
            return result;
        }
        finally
        {
            QuickJS.js_free(_ctx, ptr);
        }
    }

    /// <summary>
    /// Deserialize a JSValue from bytecode.
    /// </summary>
    /// <param name="bytecode">The bytecode to deserialize.</param>
    /// <param name="flags">Deserialization flags.</param>
    /// <returns>The deserialized value.</returns>
    public JSValue ReadObject(ReadOnlySpan<byte> bytecode, JSReadObjectFlags flags = JSReadObjectFlags.Bytecode)
    {
        fixed (byte* pBuf = bytecode)
        {
            var res = new JSValue(QuickJS.JS_ReadObject(_ctx, pBuf, (nuint)bytecode.Length, (int)flags));
            if (res.IsException)
            {
                var ex = GetException();
                throw new JSException("ReadObject failed: " + ex.ToString(this));
            }
            return res;
        }
    }

    public bool TryWriteObject(JSValue value, Span<byte> buffer, out int written, JSWriteObjectFlags flags = JSWriteObjectFlags.Bytecode)
    {
        nuint size;
        byte* ptr = QuickJS.JS_WriteObject(_ctx, &size, value.NativeValue, (int)flags);
        written = 0;

        if (ptr == null) return false;

        try
        {
            int intSize = (int)size;
            if (buffer.Length < intSize) return false;

            new ReadOnlySpan<byte>(ptr, intSize).CopyTo(buffer);
            written = intSize;
            return true;
        }
        finally
        {
            QuickJS.js_free(_ctx, ptr);
        }
    }

    /// <summary>
    /// Parse a JSON string.
    /// </summary>
    public JSValue ParseJson(string json, string filename = "input.json")
    {
        if (json is null) return default;

        int jsonMax = JSUtils.GetMaxByteCount(json.Length);
        int fileMax = JSUtils.GetMaxByteCount(filename.Length);

        byte* pJson = null;
        byte[]? jsonArray = null;
        byte* pFile = null;
        byte[]? fileArray = null;

        try
        {
            if (jsonMax <= 512)
            {
                byte* pStack = stackalloc byte[512];
                pJson = pStack;
            }
            else
            {
                jsonArray = System.Buffers.ArrayPool<byte>.Shared.Rent(jsonMax);
            }

            if (fileMax <= 512)
            {
                byte* pStack = stackalloc byte[512];
                pFile = pStack;
            }
            else
            {
                fileArray = System.Buffers.ArrayPool<byte>.Shared.Rent(fileMax);
            }

            fixed (byte* pJ = jsonArray, pF = fileArray)
            {
                byte* jPtr = pJson != null ? pJson : pJ;
                byte* fPtr = pFile != null ? pFile : pF;

                int jLen = JSUtils.GetUtf8(json, jPtr, jsonMax);
                JSUtils.GetUtf8(filename, fPtr, fileMax);

                return new JSValue(QuickJS.JS_ParseJSON(_ctx, jPtr, (nuint)jLen, fPtr));
            }
        }
        finally
        {
            if (jsonArray != null) System.Buffers.ArrayPool<byte>.Shared.Return(jsonArray);
            if (fileArray != null) System.Buffers.ArrayPool<byte>.Shared.Return(fileArray);
        }
    }

    /// <summary>
    /// Stringify a JSValue to JSON.
    /// </summary>
    public string? JSONStringify(JSValue obj, JSValue replacer = default, JSValue space = default)
    {
        JSValue strVal = new(QuickJS.JS_JSONStringify(_ctx, obj.NativeValue, replacer.NativeValue, space.NativeValue));
        if (strVal.IsException) return null;
        try { return strVal.ToString(this); }
        finally { strVal.Free(this); }
    }

    public void Dispose()
    {
        if (_ctx != null)
        {
            QuickJS.JS_FreeContext(_ctx);
            _ctx = null;
        }
        Userdata = null;
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
        GC.SuppressFinalize(this);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static QuickJS.JSValue ManagedFunctionBridge(QuickJS.JSContext* ctx, QuickJS.JSValue this_val, int argc, QuickJS.JSValue* argv, int magic, void* opaque)
    {
        GCHandle handle = GCHandle.FromIntPtr((IntPtr)opaque);
        JSFunction func = (JSFunction)handle.Target!;

        JSContext jsCtx = FromNative(ctx);
        JSValue thisWrap = new(this_val);
        ReadOnlySpan<JSValue> wrappedArgs = new(argv, argc);

        try
        {
            JSValue result = func(jsCtx, thisWrap, wrappedArgs);
            return result.NativeValue;
        }
        catch (Exception ex)
        {
            return ThrowException(ctx, ex);
        }
    }

    private static QuickJS.JSValue ThrowException(QuickJS.JSContext* ctx, Exception ex)
    {
        string msg = ex.Message;
        int maxLen = JSUtils.GetMaxByteCount(msg.Length);
        if (maxLen <= 512)
        {
            byte* pMsg = stackalloc byte[512];
            JSUtils.GetUtf8(msg, pMsg, 512);
            return QuickJS.JS_ThrowTypeError(ctx, pMsg);
        }

        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pMsg = array)
            {
                JSUtils.GetUtf8(msg, pMsg, maxLen);
                return QuickJS.JS_ThrowTypeError(ctx, pMsg);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(array);
        }
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeModuleInit(QuickJS.JSContext* ctx, QuickJS.JSModuleDef* m)
    {
        var jsCtx = JSContext.FromNative(ctx);
        IntPtr key = (IntPtr)m;

        try
        {
            // Robust routing: Ensure we call the correct initialization delegate for this specific module
            if (jsCtx._moduleInits != null && jsCtx._moduleInits.TryGetValue(key, out var init))
            {
                return init(jsCtx, new JSModule(jsCtx, m));
            }
        }
        catch (Exception ex)
        {
            // Set a JS exception so the engine knows why it failed
            jsCtx.ThrowError(ex.Message);
            return -1;
        }

        return -1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ManagedFunctionFinalizer(void* opaque)
    {
        try
        {
            GCHandle handle = GCHandle.FromIntPtr((IntPtr)opaque);
            handle.Free();
        }
        catch
        {
            // Unmanaged callers MUST NOT leak exceptions.
        }
    }

    ~JSContext() => Dispose();
}


