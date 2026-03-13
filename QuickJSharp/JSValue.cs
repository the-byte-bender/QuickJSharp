using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// A QuickJS value.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct JSValue
{
    private readonly QuickJS.JSValue _value;

    public static readonly JSValue Null = new(QuickJS.JS_NULL);
    public static readonly JSValue Undefined = new(QuickJS.JS_UNDEFINED);
    public static readonly JSValue False = new(QuickJS.JS_FALSE);
    public static readonly JSValue True = new(QuickJS.JS_TRUE);
    public static readonly JSValue Exception = new(QuickJS.JS_EXCEPTION);
    public static readonly JSValue Uninitialized = new(QuickJS.JS_UNINITIALIZED);

    /// <summary>
    /// Wraps a native JSValue and takes ownership by incrementing the reference count.
    /// </summary>
    public JSValue(JSContext ctx, QuickJS.JSValue value)
    {
        _value = QuickJS.JS_DupValue(ctx.NativeContext, value);
    }

    /// <summary>
    /// Creates a wrapper for a native JSValue.
    /// </summary>
    public JSValue(QuickJS.JSValue value)
    {
        _value = value;
    }

    public QuickJS.JSValue NativeValue => _value;
    public QuickJS.JSTag Tag => (QuickJS.JSTag)QuickJS.JS_VALUE_GET_TAG(_value);

    public bool IsNumber => Tag is QuickJS.JSTag.INT or QuickJS.JSTag.FLOAT64;
    public bool IsString => Tag is QuickJS.JSTag.STRING or QuickJS.JSTag.STRING_ROPE;
    public bool IsBoolean => Tag is QuickJS.JSTag.BOOL;
    public bool IsObject => Tag is QuickJS.JSTag.OBJECT;
    public bool IsArray => QuickJS.JS_IsArray(_value);
    public bool IsProxy => QuickJS.JS_IsProxy(_value);
    public bool IsArrayBuffer => QuickJS.JS_IsArrayBuffer(_value);
    public bool IsFunction(JSContext ctx) => QuickJS.JS_IsFunction(ctx.NativeContext, _value);
    public bool IsConstructor(JSContext ctx) => QuickJS.JS_IsConstructor(ctx.NativeContext, _value);
    public bool IsError => QuickJS.JS_IsError(_value);
    public bool IsSymbol => Tag is QuickJS.JSTag.SYMBOL;
    public bool IsBigInt => Tag is QuickJS.JSTag.BIG_INT;
    public bool IsModule => Tag is QuickJS.JSTag.MODULE;
    public bool IsNull => Tag is QuickJS.JSTag.NULL;
    public bool IsUndefined => Tag is QuickJS.JSTag.UNDEFINED;
    public bool IsException => Tag is QuickJS.JSTag.EXCEPTION;
    public bool IsUninitialized => Tag is QuickJS.JSTag.UNINITIALIZED;

    public QuickJS.JSClassID ClassID => QuickJS.JS_GetClassID(_value);
    public bool IsExtensible(JSContext ctx) => QuickJS.JS_IsExtensible(ctx.NativeContext, _value) != 0;

    private string DebuggerDisplay => Tag.ToString();

    /// <summary>
    /// Function signature for managed QuickJS callbacks.
    /// </summary>
    /// <param name="ctx">The execution context.</param>
    /// <param name="thisVal">The 'this' value (borrowed).</param>
    /// <param name="args">A span of arguments (borrowed).</param>
    /// <returns>A <see cref="JSValue"/> that the engine will take ownership of.</returns>
    public delegate JSValue JSFunction(JSContext ctx, JSValue thisVal, ReadOnlySpan<JSValue> args);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static QuickJS.JSValue ManagedFunctionBridge(QuickJS.JSContext* ctx, QuickJS.JSValue this_val, int argc, QuickJS.JSValue* argv, int magic, void* opaque)
    {
        GCHandle handle = GCHandle.FromIntPtr((IntPtr)opaque);
        JSFunction func = (JSFunction)handle.Target!;

        JSContext jsCtx = JSContext.FromNative(ctx);
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
    internal static void ManagedFunctionFinalizer(void* opaque)
    {
        GCHandle handle = GCHandle.FromIntPtr((IntPtr)opaque);
        handle.Free();
    }

    public IntPtr Opaque
    {
        get => IsObject ? (IntPtr)QuickJS.JS_GetOpaque(_value, ClassID) : IntPtr.Zero;
        set { if (IsObject) QuickJS.JS_SetOpaque(_value, (void*)value); }
    }

    public IntPtr GetOpaque(QuickJS.JSClassID classId) => IsObject ? (IntPtr)QuickJS.JS_GetOpaque(_value, classId) : IntPtr.Zero;

    public bool TryToInt32(JSContext ctx, out int result)
    {
        int res;
        if (QuickJS.JS_ToInt32(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public int ToInt32(JSContext ctx) => TryToInt32(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to Int32");

    public bool TryToUint32(JSContext ctx, out uint result)
    {
        uint res;
        if (QuickJS.JS_ToUint32(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public uint ToUint32(JSContext ctx) => TryToUint32(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to Uint32");

    public bool TryToInt64(JSContext ctx, out long result)
    {
        long res;
        if (QuickJS.JS_ToInt64Ext(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public long ToInt64(JSContext ctx) => TryToInt64(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to Int64");

    public bool TryToBigInt64(JSContext ctx, out long result)
    {
        long res;
        if (QuickJS.JS_ToBigInt64(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public long ToBigInt64(JSContext ctx) => TryToBigInt64(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to BigInt64");

    public bool TryToBigUint64(JSContext ctx, out ulong result)
    {
        ulong res;
        if (QuickJS.JS_ToBigUint64(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public ulong ToBigUint64(JSContext ctx) => TryToBigUint64(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to BigUint64");

    public bool TryToDouble(JSContext ctx, out double result)
    {
        double res;
        if (QuickJS.JS_ToFloat64(ctx.NativeContext, &res, _value) == 0)
        {
            result = res;
            return true;
        }
        result = 0;
        return false;
    }

    public double ToDouble(JSContext ctx) => TryToDouble(ctx, out var res) ? res : throw new InvalidOperationException("Cannot convert to Double");

    public bool ToBoolean(JSContext ctx)
    {
        return QuickJS.JS_ToBool(ctx.NativeContext, _value) != 0;
    }

    public string? ToString(JSContext ctx)
    {
        if (IsNull) return "null";
        if (IsUndefined) return "undefined";

        byte* ptr = QuickJS.JS_ToCString(ctx.NativeContext, _value);
        if (ptr == null) return null;
        try { return Marshal.PtrToStringUTF8((IntPtr)ptr); }
        finally { QuickJS.JS_FreeCString(ctx.NativeContext, ptr); }
    }

    /// <summary>
    /// Converts a JSValue to a string and writes it to a buffer.
    /// </summary>
    public bool TryWriteUtf8(JSContext ctx, Span<byte> buffer, out int written)
    {
        written = 0;
        if (IsNull || IsUndefined) return false;

        nuint len;
        byte* ptr = QuickJS.JS_ToCStringLen(ctx.NativeContext, &len, _value);
        if (ptr == null) return false;

        try
        {
            int intLen = (int)len;
            if (buffer.Length < intLen) return false;
            new ReadOnlySpan<byte>(ptr, intLen).CopyTo(buffer);
            written = intLen;
            return true;
        }
        finally
        {
            QuickJS.JS_FreeCString(ctx.NativeContext, ptr);
        }
    }

    public JSValue GetProperty(JSContext ctx, string name)
    {
        if (name is null) return Exception;
        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            return new JSValue(QuickJS.JS_GetPropertyStr(ctx.NativeContext, _value, pName));
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                return new JSValue(QuickJS.JS_GetPropertyStr(ctx.NativeContext, _value, pName));
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    public JSValue GetProperty(JSContext ctx, uint index)
    {
        return new JSValue(QuickJS.JS_GetPropertyUint32(ctx.NativeContext, _value, index));
    }

    public void SetProperty(JSContext ctx, string name, JSValue value)
    {
        if (name is null) return;
        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            QuickJS.JS_DupValue(ctx.NativeContext, value._value);
            if (QuickJS.JS_SetPropertyStr(ctx.NativeContext, _value, pName, value._value) < 0)
                throw new Exception("Failed to set property: " + name);
            return;
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                QuickJS.JS_DupValue(ctx.NativeContext, value._value);
                if (QuickJS.JS_SetPropertyStr(ctx.NativeContext, _value, pName, value._value) < 0)
                    throw new Exception("Failed to set property: " + name);
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    public void SetProperty(JSContext ctx, uint index, JSValue value)
    {
        QuickJS.JS_DupValue(ctx.NativeContext, value._value);
        if (QuickJS.JS_SetPropertyUint32(ctx.NativeContext, _value, index, value._value) < 0)
            throw new Exception("Failed to set property at index: " + index);
    }

    public JSValue Call(JSContext ctx, ReadOnlySpan<JSValue> args) => Call(ctx, Null, args);
    public JSValue Call(JSContext ctx, JSValue thisVal, ReadOnlySpan<JSValue> args)
    {
        fixed (JSValue* pArgs = args)
        {
            return new JSValue(QuickJS.JS_Call(ctx.NativeContext, _value, thisVal._value, args.Length, (QuickJS.JSValue*)pArgs));
        }
    }

    public JSValue Clone(JSContext ctx)
    {
        return new JSValue(QuickJS.JS_DupValue(ctx.NativeContext, _value));
    }

    public void Free(JSContext ctx)
    {
        QuickJS.JS_FreeValue(ctx.NativeContext, _value);
    }

    public static bool operator ==(JSValue left, JSValue right) =>
        left._value.tag == right._value.tag && left._value.u.ptr == right._value.u.ptr;

    public static bool operator !=(JSValue left, JSValue right) => !(left == right);
    public override bool Equals(object? obj) => obj is JSValue other && this == other;
    public override int GetHashCode() => HashCode.Combine(_value.tag, (IntPtr)_value.u.ptr);
}
