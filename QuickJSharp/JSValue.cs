using System.Diagnostics;
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
    /// Creates a wrapper for a native JSValue.
    /// </summary>
    internal JSValue(QuickJS.JSValue value)
    {
        _value = value;
    }

    public QuickJS.JSValue NativeValue => _value;
    public QuickJS.JSTag Tag => (QuickJS.JSTag)_value.tag;

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

    public JSClassID ClassID => new(QuickJS.JS_GetClassID(_value));
    public bool IsExtensible(JSContext ctx) => QuickJS.JS_IsExtensible(ctx.NativeContext, _value) != 0;

    private string DebuggerDisplay => Tag.ToString();

    public IntPtr Opaque
    {
        get
        {
            if (!IsObject) return IntPtr.Zero;
            QuickJS.JSClassID classId;
            return (IntPtr)QuickJS.JS_GetAnyOpaque(_value, &classId);
        }
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
        if (IsException)
        {
            QuickJS.JSValue exVal = QuickJS.JS_GetException(ctx.NativeContext);
            byte* exPtr = QuickJS.JS_ToCString(ctx.NativeContext, exVal);
            try { return exPtr == null ? "Exception (could not stringify)" : Marshal.PtrToStringUTF8((IntPtr)exPtr); }
            finally
            {
                if (exPtr != null) QuickJS.JS_FreeCString(ctx.NativeContext, exPtr);
                QuickJS.JS_FreeValue(ctx.NativeContext, exVal);
            }
        }

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

    /// <summary>
    /// Gets a property of the object using a <see cref="JSAtom"/>.
    /// </summary>
    public JSValue GetProperty(JSContext ctx, JSAtom atom)
    {
        return new JSValue(QuickJS.JS_GetProperty(ctx.NativeContext, _value, atom));
    }

    /// <summary>
    /// Sets a property of the object.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value to set. This function consumes the reference of the value. If you need to keep using it, pass <c>value.Duplicate(ctx)</c>.</param>
    public void SetProperty(JSContext ctx, string name, JSValue value)
    {
        if (name is null) return;
        int maxLen = JSUtils.GetMaxByteCount(name.Length);
        if (maxLen <= 512)
        {
            byte* pName = stackalloc byte[512];
            JSUtils.GetUtf8(name, pName, 512);
            if (QuickJS.JS_SetPropertyStr(ctx.NativeContext, _value, pName, value._value) < 0)
                throw new JSException("Failed to set property: " + name);
            return;
        }
        byte[] array = System.Buffers.ArrayPool<byte>.Shared.Rent(maxLen);
        try
        {
            fixed (byte* pName = array)
            {
                JSUtils.GetUtf8(name, pName, maxLen);
                if (QuickJS.JS_SetPropertyStr(ctx.NativeContext, _value, pName, value._value) < 0)
                    throw new JSException("Failed to set property: " + name);
            }
        }
        finally { System.Buffers.ArrayPool<byte>.Shared.Return(array); }
    }

    /// <summary>
    /// Sets a property of the object at the specified index.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="index">The index of the property to set.</param>
    /// <param name="value">The value to set. This function consumes the reference of the value. If you need to keep using it, pass <c>value.Duplicate(ctx)</c>.</param>
    public void SetProperty(JSContext ctx, uint index, JSValue value)
    {
        if (QuickJS.JS_SetPropertyUint32(ctx.NativeContext, _value, index, value._value) < 0)
            throw new JSException("Failed to set property at index: " + index);
    }

    /// <summary>
    /// Sets a property of the object using a <see cref="JSAtom"/>.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="atom">The <see cref="JSAtom"/> of the property to set.</param>
    /// <param name="value">The value to set. This function consumes the reference of the value. If you need to keep using it, pass <c>value.Duplicate(ctx)</c>.</param>
    public void SetProperty(JSContext ctx, JSAtom atom, JSValue value)
    {
        if (QuickJS.JS_SetProperty(ctx.NativeContext, _value, atom, value._value) < 0)
            throw new JSException("Failed to set property for atom: " + atom.Value);
    }

    /// <summary>
    /// Defines a property on this object using a name and a value.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value to define. This function consumes the reference. Use <c>value.Duplicate(ctx)</c> if you need to retain ownership.</param>
    /// <param name="flags">The property attributes (e.g. Enumerable, Configurable).</param>
    /// <remarks>
    /// This method is equivalent to <c>Object.defineProperty</c> in Javascript.
    /// </remarks>
    public void DefineProperty(JSContext ctx, string name, JSValue value, JSPropertyFlags flags = JSPropertyFlags.CWEL)
    {
        if (name is null) return;
        int len = JSUtils.GetMaxByteCount(name.Length);
        byte* pName = stackalloc byte[len];
        JSUtils.GetUtf8(name, pName, len);

        if (QuickJS.JS_DefinePropertyValueStr(ctx.NativeContext, _value, pName, value._value, (int)flags) < 0)
            throw new JSException("Failed to define property: " + name);
    }

    /// <summary>
    /// Defines a property on this object using a <see cref="JSAtom"/> and a value.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="atom">The property atom.</param>
    /// <param name="value">The value to define. This function consumes the reference.</param>
    /// <param name="flags">The property attributes.</param>
    /// <remarks>
    /// This method is equivalent to <c>Object.defineProperty</c> in Javascript.
    /// </remarks>
    public void DefineProperty(JSContext ctx, JSAtom atom, JSValue value, JSPropertyFlags flags = JSPropertyFlags.CWEL)
    {
        if (QuickJS.JS_DefinePropertyValue(ctx.NativeContext, _value, atom, value._value, (int)flags) < 0)
            throw new JSException("Failed to define property for atom: " + atom.Value);
    }

    /// <summary>
    /// Defines an accessor property (getter/setter) on this object.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="atom">The property atom.</param>
    /// <param name="getter">The getter function, or <see cref="JSValue.Undefined"/>. Consumes the reference.</param>
    /// <param name="setter">The setter function, or <see cref="JSValue.Undefined"/>. Consumes the reference.</param>
    /// <param name="flags">The accessibility flags.</param>
    /// <remarks>
    /// This is the recommended way to implement properties that wrap managed logic. 
    public void DefineProperty(JSContext ctx, JSAtom atom, JSValue getter, JSValue setter, JSPropertyFlags flags = JSPropertyFlags.Enumerable | JSPropertyFlags.Configurable)
    {
        if (QuickJS.JS_DefinePropertyGetSet(ctx.NativeContext, _value, atom, getter._value, setter._value, (int)flags) < 0)
            throw new JSException("Failed to define get/set property for atom: " + atom.Value);
    }

    /// <summary>
    /// The low-level property definition method corresponding exactly to <c>JS_DefineProperty</c>.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="atom">The property atom.</param>
    /// <param name="value">The value for a data property, or <see cref="JSValue.Undefined"/>.</param>
    /// <param name="getter">The getter function, or <see cref="JSValue.Undefined"/>.</param>
    /// <param name="setter">The setter function, or <see cref="JSValue.Undefined"/>.</param>
    /// <param name="flags">A combination of base flags and 'HAS' mask flags.</param>
    /// <remarks>
    /// This method requires explicit 'HAS' bits (e.g. <see cref="JSPropertyFlags.HasValue"/>) to be set in the <paramref name="flags"/> parameter to tell the engine which parts of the descriptor you are providing.
    /// <para>
    /// Unlike the other <c>DefineProperty</c> helpers, this method DOES NOT consume your references. 
    /// You must <c>Free(ctx)</c> on <paramref name="value"/>, <paramref name="getter"/>, and <paramref name="setter"/>, whatever is defined, separately.
    /// </para>
    /// </remarks>
    public void DefinePropertyRaw(JSContext ctx, JSAtom atom, JSValue value, JSValue getter, JSValue setter, JSPropertyFlags flags)
    {
        if (QuickJS.JS_DefineProperty(ctx.NativeContext, _value, atom, value.NativeValue, getter.NativeValue, setter.NativeValue, (int)flags) < 0)
            throw new JSException("Failed to define raw property for atom: " + atom.Value);
    }

    public JSValue Call(JSContext ctx, ReadOnlySpan<JSValue> args) => Call(ctx, Null, args);

    public JSValue Call(JSContext ctx, JSValue thisVal, ReadOnlySpan<JSValue> args)
    {
        fixed (JSValue* pArgs = args)
        {
            return new JSValue(QuickJS.JS_Call(ctx.NativeContext, _value, thisVal._value, args.Length, (QuickJS.JSValue*)pArgs));
        }
    }

    /// <summary>
    /// Sets or unsets the constructor bit of an object.
    /// </summary>
    /// <param name="ctx">The <see cref="JSContext"/> to use.</param>
    /// <param name="isConstructor">Whether the object should be considered a constructor.</param>
    /// <returns><see langword="true"/> if the bit was successfully updated, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// This method manages the internal <c>is_constructor</c> metadata of a Javascript object, which 
    /// determines if the object can be validly invoked using the <c>new</c> keyword.
    /// <para>
    /// Unlike js constructors, when a native (Dotnet) function is invoked as a constructor (via <c>new</c>, the engine does not pre-allocate a <c>this</c> object for the callback. The <c>thisVal</c> parameter passed to the callback will be the <c>new_target</c> (the constructor function itself or a derived constructor in an inheritance chain).
    ///The callback is responsible for manually allocating the new instance (See <see cref="JSContext.NewObjectProtoClass"/> with the prototype from <c>new_target</c>) and returning it.
    /// </para>
    /// </remarks>
    public bool SetConstructorBit(JSContext ctx, bool isConstructor) => QuickJS.JS_SetConstructorBit(ctx.NativeContext, _value, isConstructor);

    public JSValue Duplicate(JSContext ctx)
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
