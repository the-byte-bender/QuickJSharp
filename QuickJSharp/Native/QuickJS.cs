using System.Runtime.InteropServices;

namespace QuickJSharp.Native;

public static unsafe partial class QuickJS
{
    public const int QUICKJS_NG = 1;

    public struct JSRuntime { }

    public struct JSContext { }

    public struct JSObject { }

    public struct JSClass { }

    public struct JSClassID
    {
        public uint Value;

        public static implicit operator uint(JSClassID id) => id.Value;

        public static implicit operator JSClassID(uint value) => new JSClassID { Value = value };
    }

    public struct JSAtom
    {
        public uint Value;

        public static implicit operator uint(JSAtom atom) => atom.Value;

        public static implicit operator JSAtom(uint value) => new JSAtom { Value = value };
    }

    /* Unless documented otherwise, C string pointers (`byte*` corresponding to `char *` or `const char *`)
       are assumed to verify these constraints:
       - unless a length is passed separately, the string has a null terminator
       - string contents is either pure ASCII or is UTF-8 encoded.
     */

    public enum JSTag : long
    {
        /* all tags with a reference count are negative */
        FIRST = -9, /* first negative tag */
        BIG_INT = -9,
        SYMBOL = -8,
        STRING = -7,
        STRING_ROPE = -6,
        MODULE = -3, /* used internally */
        FUNCTION_BYTECODE = -2, /* used internally */
        OBJECT = -1,

        INT = 0,
        BOOL = 1,
        NULL = 2,
        UNDEFINED = 3,
        UNINITIALIZED = 4,
        CATCH_OFFSET = 5,
        EXCEPTION = 6,
        SHORT_BIG_INT = 7,
        FLOAT64 = 8,
        /* any larger tag is FLOAT64 if JS_NAN_BOXING */
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct JSValueUnion
    {
        [FieldOffset(0)]
        public int int32;

        [FieldOffset(0)]
        public double float64;

        [FieldOffset(0)]
        public void* ptr;

        [FieldOffset(0)]
        public int short_big_int;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSValue
    {
        public JSValueUnion u;
        public long tag;
    }

    // Macro Helpers
    public static int JS_VALUE_GET_TAG(JSValue v) => (int)v.tag;

    public static int JS_VALUE_GET_NORM_TAG(JSValue v) => (int)v.tag;

    public static int JS_VALUE_GET_INT(JSValue v) => v.u.int32;

    public static bool JS_VALUE_GET_BOOL(JSValue v) => v.u.int32 != 0;

    public static double JS_VALUE_GET_FLOAT64(JSValue v) => v.u.float64;

    public static int JS_VALUE_GET_SHORT_BIG_INT(JSValue v) => v.u.short_big_int;

    public static void* JS_VALUE_GET_PTR(JSValue v) => v.u.ptr;

    public static JSValue JS_MKPTR(long tag, void* ptr)
    {
        JSValue v = default;
        v.u.ptr = ptr;
        v.tag = tag;
        return v;
    }

    public static JSValue JS_MKVAL(long tag, int int32)
    {
        JSValue v = default;
        v.u.int32 = int32;
        v.tag = tag;
        return v;
    }

    public static JSValue JS_MKNAN()
    {
        JSValue v = default;
        v.u.float64 = double.NaN;
        v.tag = (long)JSTag.FLOAT64;
        return v;
    }

    public static readonly JSValue JS_NAN = JS_MKNAN();

    public static bool JS_TAG_IS_FLOAT64(long tag) => tag == (long)JSTag.FLOAT64;

    public static JSValue __JS_NewFloat64(double d)
    {
        JSValue v = default;
        v.tag = (long)JSTag.FLOAT64;
        v.u.float64 = d;
        return v;
    }

    public static JSValue __JS_NewShortBigInt(JSContext* ctx, long d)
    {
        JSValue v = default;
        v.tag = (long)JSTag.SHORT_BIG_INT;
        v.u.short_big_int = (int)d;
        return v;
    }

    public static bool JS_VALUE_IS_NAN(JSValue v)
    {
        if (v.tag != (long)JSTag.FLOAT64)
            return false;
        return double.IsNaN(v.u.float64); // Abstracting bit mask to C#'s built in IsNaN
    }

    public static bool JS_VALUE_IS_BOTH_INT(JSValue v1, JSValue v2) =>
        ((JS_VALUE_GET_TAG(v1) | JS_VALUE_GET_TAG(v2)) == 0);

    public static bool JS_VALUE_IS_BOTH_FLOAT(JSValue v1, JSValue v2) =>
        JS_TAG_IS_FLOAT64(JS_VALUE_GET_TAG(v1)) && JS_TAG_IS_FLOAT64(JS_VALUE_GET_TAG(v2));

    public static bool JS_VALUE_HAS_REF_COUNT(JSValue v) => (uint)JS_VALUE_GET_TAG(v) >= unchecked((uint)JSTag.FIRST);

    public static readonly JSValue JS_NULL = JS_MKVAL((long)JSTag.NULL, 0);
    public static readonly JSValue JS_UNDEFINED = JS_MKVAL((long)JSTag.UNDEFINED, 0);
    public static readonly JSValue JS_FALSE = JS_MKVAL((long)JSTag.BOOL, 0);
    public static readonly JSValue JS_TRUE = JS_MKVAL((long)JSTag.BOOL, 1);
    public static readonly JSValue JS_EXCEPTION = JS_MKVAL((long)JSTag.EXCEPTION, 0);
    public static readonly JSValue JS_UNINITIALIZED = JS_MKVAL((long)JSTag.UNINITIALIZED, 0);

    /* flags for object properties */
    public const int JS_PROP_CONFIGURABLE = (1 << 0);
    public const int JS_PROP_WRITABLE = (1 << 1);
    public const int JS_PROP_ENUMERABLE = (1 << 2);
    public const int JS_PROP_C_W_E = (JS_PROP_CONFIGURABLE | JS_PROP_WRITABLE | JS_PROP_ENUMERABLE);
    public const int JS_PROP_LENGTH = (1 << 3); /* used internally in Arrays */
    public const int JS_PROP_TMASK = (3 << 4); /* mask for NORMAL, GETSET, VARREF, AUTOINIT */
    public const int JS_PROP_NORMAL = (0 << 4);
    public const int JS_PROP_GETSET = (1 << 4);
    public const int JS_PROP_VARREF = (2 << 4); /* used internally */
    public const int JS_PROP_AUTOINIT = (3 << 4); /* used internally */

    /* flags for JS_DefineProperty */
    public const int JS_PROP_HAS_SHIFT = 8;
    public const int JS_PROP_HAS_CONFIGURABLE = (1 << 8);
    public const int JS_PROP_HAS_WRITABLE = (1 << 9);
    public const int JS_PROP_HAS_ENUMERABLE = (1 << 10);
    public const int JS_PROP_HAS_GET = (1 << 11);
    public const int JS_PROP_HAS_SET = (1 << 12);
    public const int JS_PROP_HAS_VALUE = (1 << 13);

    /* throw an exception if false would be returned (JS_DefineProperty/JS_SetProperty) */
    public const int JS_PROP_THROW = (1 << 14);

    /* throw an exception if false would be returned in strict mode (JS_SetProperty) */
    public const int JS_PROP_THROW_STRICT = (1 << 15);

    public const int JS_PROP_NO_ADD = (1 << 16); /* internal use */
    public const int JS_PROP_NO_EXOTIC = (1 << 17); /* internal use */
    public const int JS_PROP_DEFINE_PROPERTY = (1 << 18); /* internal use */
    public const int JS_PROP_REFLECT_DEFINE_PROPERTY = (1 << 19); /* internal use */

    public const int JS_DEFAULT_STACK_SIZE = (1024 * 1024);

    /* JS_Eval() flags */
    public const int JS_EVAL_TYPE_GLOBAL = (0 << 0); /* global code (default) */
    public const int JS_EVAL_TYPE_MODULE = (1 << 0); /* module code */
    public const int JS_EVAL_TYPE_DIRECT = (2 << 0); /* direct call (internal use) */
    public const int JS_EVAL_TYPE_INDIRECT = (3 << 0); /* indirect call (internal use) */
    public const int JS_EVAL_TYPE_MASK = (3 << 0);

    public const int JS_EVAL_FLAG_STRICT = (1 << 3); /* force 'strict' mode */
    public const int JS_EVAL_FLAG_UNUSED = (1 << 4); /* unused */

    /* compile but do not run. The result is an object with a JS_TAG_FUNCTION_BYTECODE or JS_TAG_MODULE tag. It can be executed with JS_EvalFunction(). */
    public const int JS_EVAL_FLAG_COMPILE_ONLY = (1 << 5);

    /* don't include the stack frames before this eval in the Error() backtraces */
    public const int JS_EVAL_FLAG_BACKTRACE_BARRIER = (1 << 6);

    /* allow top-level await in normal script. JS_Eval() returns a promise. Only allowed with JS_EVAL_TYPE_GLOBAL */
    public const int JS_EVAL_FLAG_ASYNC = (1 << 7);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSMallocFunctions
    {
        public delegate* unmanaged[Cdecl]<void*, nuint, nuint, void*> js_calloc;
        public delegate* unmanaged[Cdecl]<void*, nuint, void*> js_malloc;
        public delegate* unmanaged[Cdecl]<void*, void*, void> js_free;
        public delegate* unmanaged[Cdecl]<void*, void*, nuint, void*> js_realloc;
        public delegate* unmanaged[Cdecl]<void*, nuint> js_malloc_usable_size;
    }

    // Debug trace system
    public const int JS_DUMP_BYTECODE_FINAL = 0x01; /* dump pass 3 final byte code */
    public const int JS_DUMP_BYTECODE_PASS2 = 0x02; /* dump pass 2 code */
    public const int JS_DUMP_BYTECODE_PASS1 = 0x04; /* dump pass 1 code */
    public const int JS_DUMP_BYTECODE_HEX = 0x10; /* dump bytecode in hex */
    public const int JS_DUMP_BYTECODE_PC2LINE = 0x20; /* dump line number table */
    public const int JS_DUMP_BYTECODE_STACK = 0x40; /* dump compute_stack_size */
    public const int JS_DUMP_BYTECODE_STEP = 0x80; /* dump executed bytecode */
    public const int JS_DUMP_READ_OBJECT = 0x100; /* dump the marshalled objects at load time */
    public const int JS_DUMP_FREE = 0x200; /* dump every object free */
    public const int JS_DUMP_GC = 0x400; /* dump the occurrence of the automatic GC */
    public const int JS_DUMP_GC_FREE = 0x800; /* dump objects freed by the GC */
    public const int JS_DUMP_MODULE_RESOLVE = 0x1000; /* dump module resolution steps */
    public const int JS_DUMP_PROMISE = 0x2000; /* dump promise steps */
    public const int JS_DUMP_LEAKS = 0x4000; /* dump leaked objects and strings in JS_FreeRuntime */
    public const int JS_DUMP_ATOM_LEAKS = 0x8000; /* dump leaked atoms in JS_FreeRuntime */
    public const int JS_DUMP_MEM = 0x10000; /* dump memory usage in JS_FreeRuntime */
    public const int JS_DUMP_OBJECTS = 0x20000; /* dump objects in JS_FreeRuntime */
    public const int JS_DUMP_ATOMS = 0x40000; /* dump atoms in JS_FreeRuntime */
    public const int JS_DUMP_SHAPES = 0x80000; /* dump shapes in JS_FreeRuntime */

    public struct JSGCObjectHeader { } // Opaque

    [LibraryImport("quickjs")]
    public static partial JSRuntime* JS_NewRuntime();

    [LibraryImport("quickjs")]
    public static partial void JS_SetRuntimeInfo(JSRuntime* rt, byte* info);

    [LibraryImport("quickjs")]
    public static partial void JS_SetMemoryLimit(JSRuntime* rt, nuint limit);

    [LibraryImport("quickjs")]
    public static partial void JS_SetDumpFlags(JSRuntime* rt, ulong flags);

    [LibraryImport("quickjs")]
    public static partial ulong JS_GetDumpFlags(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial nuint JS_GetGCThreshold(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial void JS_SetGCThreshold(JSRuntime* rt, nuint gc_threshold);

    [LibraryImport("quickjs")]
    public static partial void JS_SetMaxStackSize(JSRuntime* rt, nuint stack_size);

    [LibraryImport("quickjs")]
    public static partial void JS_UpdateStackTop(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial JSRuntime* JS_NewRuntime2(JSMallocFunctions* mf, void* opaque);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeRuntime(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial void* JS_GetRuntimeOpaque(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial void JS_SetRuntimeOpaque(JSRuntime* rt, void* opaque);

    [LibraryImport("quickjs")]
    public static partial int JS_AddRuntimeFinalizer(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSRuntime*, void*, void> finalizer,
        void* arg
    );

    [LibraryImport("quickjs")]
    public static partial void JS_MarkValue(
        JSRuntime* rt,
        JSValue val,
        delegate* unmanaged[Cdecl]<JSRuntime*, JSGCObjectHeader*, void> mark_func
    );

    [LibraryImport("quickjs")]
    public static partial void JS_RunGC(JSRuntime* rt);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsLiveObject(JSRuntime* rt, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial JSContext* JS_NewContext(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeContext(JSContext* s);

    [LibraryImport("quickjs")]
    public static partial JSContext* JS_DupContext(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void* JS_GetContextOpaque(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_SetContextOpaque(JSContext* ctx, void* opaque);

    [LibraryImport("quickjs")]
    public static partial JSRuntime* JS_GetRuntime(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_SetClassProto(JSContext* ctx, JSClassID class_id, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetClassProto(JSContext* ctx, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetFunctionProto(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial JSContext* JS_NewContextRaw(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicBaseObjects(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicDate(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicEval(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicRegExpCompiler(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial int JS_AddIntrinsicRegExp(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicJSON(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicProxy(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicMapSet(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicTypedArrays(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicPromise(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicBigInt(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicWeakRef(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddPerformance(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_AddIntrinsicDOMException(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial int JS_IsEqual(JSContext* ctx, JSValue op1, JSValue op2);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsStrictEqual(JSContext* ctx, JSValue op1, JSValue op2);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsSameValue(JSContext* ctx, JSValue op1, JSValue op2);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsSameValueZero(JSContext* ctx, JSValue op1, JSValue op2);

    [LibraryImport("quickjs")]
    public static partial JSValue js_string_codePointRange(JSContext* ctx, JSValue this_val, int argc, JSValue* argv);

    [LibraryImport("quickjs")]
    public static partial void* js_calloc_rt(JSRuntime* rt, nuint count, nuint size);

    [LibraryImport("quickjs")]
    public static partial void* js_malloc_rt(JSRuntime* rt, nuint size);

    [LibraryImport("quickjs")]
    public static partial void js_free_rt(JSRuntime* rt, void* ptr);

    [LibraryImport("quickjs")]
    public static partial void* js_realloc_rt(JSRuntime* rt, void* ptr, nuint size);

    [LibraryImport("quickjs")]
    public static partial nuint js_malloc_usable_size_rt(JSRuntime* rt, void* ptr);

    [LibraryImport("quickjs")]
    public static partial void* js_mallocz_rt(JSRuntime* rt, nuint size);

    [LibraryImport("quickjs")]
    public static partial void* js_calloc(JSContext* ctx, nuint count, nuint size);

    [LibraryImport("quickjs")]
    public static partial void* js_malloc(JSContext* ctx, nuint size);

    [LibraryImport("quickjs")]
    public static partial void js_free(JSContext* ctx, void* ptr);

    [LibraryImport("quickjs")]
    public static partial void* js_realloc(JSContext* ctx, void* ptr, nuint size);

    [LibraryImport("quickjs")]
    public static partial nuint js_malloc_usable_size(JSContext* ctx, void* ptr);

    [LibraryImport("quickjs")]
    public static partial void* js_realloc2(JSContext* ctx, void* ptr, nuint size, nuint* pslack);

    [LibraryImport("quickjs")]
    public static partial void* js_mallocz(JSContext* ctx, nuint size);

    [LibraryImport("quickjs")]
    public static partial byte* js_strdup(JSContext* ctx, byte* str);

    [LibraryImport("quickjs")]
    public static partial byte* js_strndup(JSContext* ctx, byte* s, nuint n);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSMemoryUsage
    {
        public long malloc_size,
            malloc_limit,
            memory_used_size;
        public long malloc_count;
        public long memory_used_count;
        public long atom_count,
            atom_size;
        public long str_count,
            str_size;
        public long obj_count,
            obj_size;
        public long prop_count,
            prop_size;
        public long shape_count,
            shape_size;
        public long js_func_count,
            js_func_size,
            js_func_code_size;
        public long js_func_pc2line_count,
            js_func_pc2line_size;
        public long c_func_count,
            array_count;
        public long fast_array_count,
            fast_array_elements;
        public long binary_object_count,
            binary_object_size;
    }

    [LibraryImport("quickjs")]
    public static partial void JS_ComputeMemoryUsage(JSRuntime* rt, JSMemoryUsage* s);

    [LibraryImport("quickjs")]
    public static partial void JS_DumpMemoryUsage(void* fp, JSMemoryUsage* s, JSRuntime* rt);

    public const uint JS_ATOM_NULL = 0;

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_NewAtomLen(JSContext* ctx, byte* str, nuint len);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_NewAtom(JSContext* ctx, byte* str);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_NewAtomUInt32(JSContext* ctx, uint n);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_DupAtom(JSContext* ctx, JSAtom v);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_DupAtomRT(JSRuntime* rt, JSAtom v);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeAtom(JSContext* ctx, JSAtom v);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeAtomRT(JSRuntime* rt, JSAtom v);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_AtomToValue(JSContext* ctx, JSAtom atom);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_AtomToString(JSContext* ctx, JSAtom atom);

    [LibraryImport("quickjs")]
    public static partial byte* JS_AtomToCStringLen(JSContext* ctx, nuint* plen, JSAtom atom);

    public static byte* JS_AtomToCString(JSContext* ctx, JSAtom atom)
    {
        return JS_AtomToCStringLen(ctx, null, atom);
    }

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_ValueToAtom(JSContext* ctx, JSValue val);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSPropertyEnum
    {
        public byte is_enumerable; // mapped from bool, safer for StructLayout
        public JSAtom atom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSPropertyDescriptor
    {
        public int flags;
        public JSValue value;
        public JSValue getter;
        public JSValue setter;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSClassExoticMethods
    {
        public delegate* unmanaged[Cdecl]<JSContext*, JSPropertyDescriptor*, JSValue, JSAtom, int> get_own_property;
        public delegate* unmanaged[Cdecl]<JSContext*, JSPropertyEnum**, uint*, JSValue, int> get_own_property_names;
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSAtom, int> delete_property;
        public delegate* unmanaged[Cdecl]<
            JSContext*,
            JSValue,
            JSAtom,
            JSValue,
            JSValue,
            JSValue,
            int,
            int> define_own_property;
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSAtom, int> has_property;
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSAtom, JSValue, JSValue> get_property;
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSAtom, JSValue, JSValue, int, int> set_property;
    }

    public const int JS_CALL_FLAG_CONSTRUCTOR = (1 << 0);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSClassDef
    {
        public byte* class_name;
        public delegate* unmanaged[Cdecl]<JSRuntime*, JSValue, void> finalizer;
        public delegate* unmanaged[Cdecl]<
            JSRuntime*,
            JSValue,
            delegate* unmanaged[Cdecl]<JSRuntime*, JSGCObjectHeader*, void>,
            void> gc_mark;
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSValue, int, JSValue*, int, JSValue> call;
        public JSClassExoticMethods* exotic;
    }

    public const int JS_EVAL_OPTIONS_VERSION = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct JSEvalOptions
    {
        public int version;
        public int eval_flags;
        public byte* filename;
        public int line_num;
    }

    public const uint JS_INVALID_CLASS_ID = 0;

    [LibraryImport("quickjs")]
    public static partial JSClassID JS_NewClassID(JSRuntime* rt, JSClassID* pclass_id);

    [LibraryImport("quickjs")]
    public static partial JSClassID JS_GetClassID(JSValue v);

    [LibraryImport("quickjs")]
    public static partial int JS_NewClass(JSRuntime* rt, JSClassID class_id, JSClassDef* class_def);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsRegisteredClass(JSRuntime* rt, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_GetClassName(JSRuntime* rt, JSClassID class_id);

    public static JSValue JS_NewBool(JSContext* ctx, bool val) => JS_MKVAL((long)JSTag.BOOL, val ? 1 : 0);

    public static JSValue JS_NewInt32(JSContext* ctx, int val) => JS_MKVAL((long)JSTag.INT, val);

    public static JSValue JS_NewFloat64(JSContext* ctx, double val) => __JS_NewFloat64(val);

    public static JSValue JS_NewCatchOffset(JSContext* ctx, int val) => JS_MKVAL((long)JSTag.CATCH_OFFSET, val);

    public static JSValue JS_NewInt64(JSContext* ctx, long val)
    {
        if (val >= int.MinValue && val <= int.MaxValue)
            return JS_NewInt32(ctx, (int)val);
        return JS_NewFloat64(ctx, (double)val);
    }

    public static JSValue JS_NewUint32(JSContext* ctx, uint val)
    {
        if (val <= int.MaxValue)
            return JS_NewInt32(ctx, (int)val);
        return JS_NewFloat64(ctx, (double)val);
    }

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewNumber(JSContext* ctx, double d);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewBigInt64(JSContext* ctx, long v);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewBigUint64(JSContext* ctx, ulong v);

    public static bool JS_IsNumber(JSValue v)
    {
        int tag = JS_VALUE_GET_TAG(v);
        return tag == (int)JSTag.INT || JS_TAG_IS_FLOAT64(tag);
    }

    public static bool JS_IsBigInt(JSValue v)
    {
        int tag = JS_VALUE_GET_TAG(v);
        return tag == (int)JSTag.BIG_INT || tag == (int)JSTag.SHORT_BIG_INT;
    }

    public static bool JS_IsBool(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.BOOL;

    public static bool JS_IsNull(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.NULL;

    public static bool JS_IsUndefined(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.UNDEFINED;

    public static bool JS_IsException(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.EXCEPTION;

    public static bool JS_IsUninitialized(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.UNINITIALIZED;

    public static bool JS_IsString(JSValue v)
    {
        int tag = JS_VALUE_GET_TAG(v);
        return tag == (int)JSTag.STRING || tag == (int)JSTag.STRING_ROPE;
    }

    public static bool JS_IsSymbol(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.SYMBOL;

    public static bool JS_IsObject(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.OBJECT;

    public static bool JS_IsModule(JSValue v) => JS_VALUE_GET_TAG(v) == (int)JSTag.MODULE;

    [LibraryImport("quickjs")]
    public static partial JSValue JS_Throw(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetException(JSContext* ctx);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_HasException(JSContext* ctx);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsError(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsUncatchableError(JSValue val);

    [LibraryImport("quickjs")]
    public static partial void JS_SetUncatchableError(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial void JS_ClearUncatchableError(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial void JS_ResetUncatchableError(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewError(JSContext* ctx);

    // Note: Varargs (...) mapped only to the required `fmt` parameter.
    // It is recommended to use managed string formatting and pass the final string.
    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewInternalError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewPlainError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewRangeError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewReferenceError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewSyntaxError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewTypeError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowInternalError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowPlainError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowRangeError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowReferenceError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowSyntaxError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowTypeError(JSContext* ctx, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowDOMException(JSContext* ctx, byte* name, byte* fmt);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ThrowOutOfMemory(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeValue(JSContext* ctx, JSValue v);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeValueRT(JSRuntime* rt, JSValue v);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_DupValue(JSContext* ctx, JSValue v);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_DupValueRT(JSRuntime* rt, JSValue v);

    [LibraryImport("quickjs")]
    public static partial int JS_ToBool(JSContext* ctx, JSValue val);

    public static JSValue JS_ToBoolean(JSContext* ctx, JSValue val) => JS_NewBool(ctx, JS_ToBool(ctx, val) != 0);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ToNumber(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToInt32(JSContext* ctx, int* pres, JSValue val);

    public static int JS_ToUint32(JSContext* ctx, uint* pres, JSValue val) => JS_ToInt32(ctx, (int*)pres, val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToInt64(JSContext* ctx, long* pres, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToIndex(JSContext* ctx, ulong* plen, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToFloat64(JSContext* ctx, double* pres, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToBigInt64(JSContext* ctx, long* pres, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToBigUint64(JSContext* ctx, ulong* pres, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_ToInt64Ext(JSContext* ctx, long* pres, JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewStringLen(JSContext* ctx, byte* str1, nuint len1);

    public static JSValue JS_NewString(JSContext* ctx, byte* str)
    {
        nuint len = 0;
        while (str[len] != 0)
            len++;
        return JS_NewStringLen(ctx, str, len);
    }

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewStringUTF16(JSContext* ctx, ushort* buf, nuint len);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewAtomString(JSContext* ctx, byte* str);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ToString(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ToPropertyKey(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial byte* JS_ToCStringLen2(
        JSContext* ctx,
        nuint* plen,
        JSValue val1,
        [MarshalAs(UnmanagedType.I1)] bool cesu8
    );

    public static byte* JS_ToCStringLen(JSContext* ctx, nuint* plen, JSValue val1) =>
        JS_ToCStringLen2(ctx, plen, val1, false);

    public static byte* JS_ToCString(JSContext* ctx, JSValue val1) => JS_ToCStringLen2(ctx, null, val1, false);

    [LibraryImport("quickjs")]
    public static partial ushort* JS_ToCStringLenUTF16(JSContext* ctx, nuint* plen, JSValue val1);

    public static ushort* JS_ToCStringUTF16(JSContext* ctx, JSValue val1) => JS_ToCStringLenUTF16(ctx, null, val1);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeCString(JSContext* ctx, byte* ptr);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeCStringRT(JSRuntime* rt, byte* ptr);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeCStringUTF16(JSContext* ctx, ushort* ptr);

    [LibraryImport("quickjs")]
    public static partial void JS_FreeCStringRT_UTF16(JSRuntime* rt, ushort* ptr);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObjectProtoClass(JSContext* ctx, JSValue proto, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObjectClass(JSContext* ctx, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObjectProto(JSContext* ctx, JSValue proto);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObject(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObjectFrom(JSContext* ctx, int count, JSAtom* props, JSValue* values);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewObjectFromStr(JSContext* ctx, int count, byte** props, JSValue* values);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ToObject(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ToObjectString(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsFunction(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsConstructor(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_SetConstructorBit(
        JSContext* ctx,
        JSValue func_obj,
        [MarshalAs(UnmanagedType.I1)] bool val
    );

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsRegExp(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsMap(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsSet(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsWeakRef(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsWeakSet(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsWeakMap(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsDataView(JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewArray(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewArrayFrom(JSContext* ctx, int count, JSValue* values);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsArray(JSValue val);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsProxy(JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetProxyTarget(JSContext* ctx, JSValue proxy);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetProxyHandler(JSContext* ctx, JSValue proxy);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewProxy(JSContext* ctx, JSValue target, JSValue handler);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewDate(JSContext* ctx, double epoch_ms);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsDate(JSValue v);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetProperty(JSContext* ctx, JSValue this_obj, JSAtom prop);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetPropertyUint32(JSContext* ctx, JSValue this_obj, uint idx);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetPropertyInt64(JSContext* ctx, JSValue this_obj, long idx);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetPropertyStr(JSContext* ctx, JSValue this_obj, byte* prop);

    [LibraryImport("quickjs")]
    public static partial int JS_SetProperty(JSContext* ctx, JSValue this_obj, JSAtom prop, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_SetPropertyUint32(JSContext* ctx, JSValue this_obj, uint idx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_SetPropertyInt64(JSContext* ctx, JSValue this_obj, long idx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_SetPropertyStr(JSContext* ctx, JSValue this_obj, byte* prop, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_HasProperty(JSContext* ctx, JSValue this_obj, JSAtom prop);

    [LibraryImport("quickjs")]
    public static partial int JS_IsExtensible(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_PreventExtensions(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_DeleteProperty(JSContext* ctx, JSValue obj, JSAtom prop, int flags);

    [LibraryImport("quickjs")]
    public static partial int JS_SetPrototype(JSContext* ctx, JSValue obj, JSValue proto_val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetPrototype(JSContext* ctx, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_GetLength(JSContext* ctx, JSValue obj, long* pres);

    [LibraryImport("quickjs")]
    public static partial int JS_SetLength(JSContext* ctx, JSValue obj, long len);

    [LibraryImport("quickjs")]
    public static partial int JS_SealObject(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_FreezeObject(JSContext* ctx, JSValue obj);

    public const int JS_GPN_STRING_MASK = (1 << 0);
    public const int JS_GPN_SYMBOL_MASK = (1 << 1);
    public const int JS_GPN_PRIVATE_MASK = (1 << 2);
    public const int JS_GPN_ENUM_ONLY = (1 << 4);
    public const int JS_GPN_SET_ENUM = (1 << 5);

    [LibraryImport("quickjs")]
    public static partial int JS_GetOwnPropertyNames(
        JSContext* ctx,
        JSPropertyEnum** ptab,
        uint* plen,
        JSValue obj,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_GetOwnProperty(JSContext* ctx, JSPropertyDescriptor* desc, JSValue obj, JSAtom prop);

    [LibraryImport("quickjs")]
    public static partial void JS_FreePropertyEnum(JSContext* ctx, JSPropertyEnum* tab, uint len);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_Call(JSContext* ctx, JSValue func_obj, JSValue this_obj, int argc, JSValue* argv);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_Invoke(JSContext* ctx, JSValue this_val, JSAtom atom, int argc, JSValue* argv);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_CallConstructor(JSContext* ctx, JSValue func_obj, int argc, JSValue* argv);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_CallConstructor2(
        JSContext* ctx,
        JSValue func_obj,
        JSValue new_target,
        int argc,
        JSValue* argv
    );

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_DetectModule(byte* input, nuint input_len);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_Eval(JSContext* ctx, byte* input, nuint input_len, byte* filename, int eval_flags);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_Eval2(JSContext* ctx, byte* input, nuint input_len, JSEvalOptions* options);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_EvalThis(
        JSContext* ctx,
        JSValue this_obj,
        byte* input,
        nuint input_len,
        byte* filename,
        int eval_flags
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_EvalThis2(
        JSContext* ctx,
        JSValue this_obj,
        byte* input,
        nuint input_len,
        JSEvalOptions* options
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetGlobalObject(JSContext* ctx);

    [LibraryImport("quickjs")]
    public static partial int JS_IsInstanceOf(JSContext* ctx, JSValue val, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_DefineProperty(
        JSContext* ctx,
        JSValue this_obj,
        JSAtom prop,
        JSValue val,
        JSValue getter,
        JSValue setter,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_DefinePropertyValue(
        JSContext* ctx,
        JSValue this_obj,
        JSAtom prop,
        JSValue val,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_DefinePropertyValueUint32(
        JSContext* ctx,
        JSValue this_obj,
        uint idx,
        JSValue val,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_DefinePropertyValueStr(
        JSContext* ctx,
        JSValue this_obj,
        byte* prop,
        JSValue val,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_DefinePropertyGetSet(
        JSContext* ctx,
        JSValue this_obj,
        JSAtom prop,
        JSValue getter,
        JSValue setter,
        int flags
    );

    [LibraryImport("quickjs")]
    public static partial int JS_SetOpaque(JSValue obj, void* opaque);

    [LibraryImport("quickjs")]
    public static partial void* JS_GetOpaque(JSValue obj, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial void* JS_GetOpaque2(JSContext* ctx, JSValue obj, JSClassID class_id);

    [LibraryImport("quickjs")]
    public static partial void* JS_GetAnyOpaque(JSValue obj, JSClassID* class_id);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ParseJSON(JSContext* ctx, byte* buf, nuint buf_len, byte* filename);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_JSONStringify(JSContext* ctx, JSValue obj, JSValue replacer, JSValue space0);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewArrayBuffer(
        JSContext* ctx,
        byte* buf,
        nuint len,
        delegate* unmanaged[Cdecl]<JSRuntime*, void*, void*, void> free_func,
        void* opaque,
        [MarshalAs(UnmanagedType.I1)] bool is_shared
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewArrayBufferCopy(JSContext* ctx, byte* buf, nuint len);

    [LibraryImport("quickjs")]
    public static partial void JS_DetachArrayBuffer(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial byte* JS_GetArrayBuffer(JSContext* ctx, nuint* psize, JSValue obj);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsArrayBuffer(JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_IsImmutableArrayBuffer(JSValue obj);

    [LibraryImport("quickjs")]
    public static partial int JS_SetImmutableArrayBuffer(JSValue obj, [MarshalAs(UnmanagedType.I1)] bool immutable);

    [LibraryImport("quickjs")]
    public static partial byte* JS_GetUint8Array(JSContext* ctx, nuint* psize, JSValue obj);

    public enum JSTypedArrayEnum
    {
        UINT8C = 0,
        INT8,
        UINT8,
        INT16,
        UINT16,
        INT32,
        UINT32,
        BIG_INT64,
        BIG_UINT64,
        FLOAT16,
        FLOAT32,
        FLOAT64,
    }

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewTypedArray(
        JSContext* ctx,
        int argc,
        JSValue* argv,
        JSTypedArrayEnum array_type
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetTypedArrayBuffer(
        JSContext* ctx,
        JSValue obj,
        nuint* pbyte_offset,
        nuint* pbyte_length,
        nuint* pbytes_per_element
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewUint8Array(
        JSContext* ctx,
        byte* buf,
        nuint len,
        delegate* unmanaged[Cdecl]<JSRuntime*, void*, void*, void> free_func,
        void* opaque,
        [MarshalAs(UnmanagedType.I1)] bool is_shared
    );

    [LibraryImport("quickjs")]
    public static partial int JS_GetTypedArrayType(JSValue obj);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewUint8ArrayCopy(JSContext* ctx, byte* buf, nuint len);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSSharedArrayBufferFunctions
    {
        public delegate* unmanaged[Cdecl]<void*, nuint, void*> sab_alloc;
        public delegate* unmanaged[Cdecl]<void*, void*, void> sab_free;
        public delegate* unmanaged[Cdecl]<void*, void*, void> sab_dup;
        public void* sab_opaque;
    }

    [LibraryImport("quickjs")]
    public static partial void JS_SetSharedArrayBufferFunctions(JSRuntime* rt, JSSharedArrayBufferFunctions* sf);

    public enum JSPromiseStateEnum
    {
        NOT_A_PROMISE = -1,
        PENDING = 0,
        FULFILLED,
        REJECTED,
    }

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewPromiseCapability(JSContext* ctx, JSValue* resolving_funcs);

    [LibraryImport("quickjs")]
    public static partial JSPromiseStateEnum JS_PromiseState(JSContext* ctx, JSValue promise);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_PromiseResult(JSContext* ctx, JSValue promise);

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsPromise(JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewSettledPromise(
        JSContext* ctx,
        [MarshalAs(UnmanagedType.I1)] bool is_reject,
        JSValue value
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewSymbol(
        JSContext* ctx,
        byte* description,
        [MarshalAs(UnmanagedType.I1)] bool is_global
    );

    public enum JSPromiseHookType
    {
        INIT,
        BEFORE,
        AFTER,
        RESOLVE,
    }

    [LibraryImport("quickjs")]
    public static partial void JS_SetPromiseHook(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSContext*, JSPromiseHookType, JSValue, JSValue, void*, void> promise_hook,
        void* opaque
    );

    [LibraryImport("quickjs")]
    public static partial void JS_SetHostPromiseRejectionTracker(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSValue, byte, void*, void> cb,
        void* opaque
    );

    [LibraryImport("quickjs")]
    public static partial void JS_SetInterruptHandler(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSRuntime*, void*, int> cb,
        void* opaque
    );

    [LibraryImport("quickjs")]
    public static partial void JS_SetCanBlock(JSRuntime* rt, [MarshalAs(UnmanagedType.I1)] bool can_block);

    [LibraryImport("quickjs")]
    public static partial void JS_SetIsHTMLDDA(JSContext* ctx, JSValue obj);

    public struct JSModuleDef { }

    [LibraryImport("quickjs")]
    public static partial void JS_SetModuleLoaderFunc(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSContext*, byte*, byte*, void*, byte*> module_normalize,
        delegate* unmanaged[Cdecl]<JSContext*, byte*, void*, JSModuleDef*> module_loader,
        void* opaque
    );

    [LibraryImport("quickjs")]
    public static partial void JS_SetModuleLoaderFunc2(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSContext*, byte*, byte*, JSValue, void*, byte*> module_normalize,
        delegate* unmanaged[Cdecl]<JSContext*, byte*, void*, JSValue, JSModuleDef*> module_loader,
        delegate* unmanaged[Cdecl]<JSContext*, void*, JSValue, int> module_check_attrs,
        void* opaque
    );

    [LibraryImport("quickjs")]
    public static partial void JS_SetModuleNormalizeFunc2(
        JSRuntime* rt,
        delegate* unmanaged[Cdecl]<JSContext*, byte*, byte*, JSValue, void*, byte*> module_normalize
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetImportMeta(JSContext* ctx, JSModuleDef* m);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_GetModuleName(JSContext* ctx, JSModuleDef* m);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetModuleNamespace(JSContext* ctx, JSModuleDef* m);

    [LibraryImport("quickjs")]
    public static partial int JS_SetModulePrivateValue(JSContext* ctx, JSModuleDef* m, JSValue val);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_GetModulePrivateValue(JSContext* ctx, JSModuleDef* m);

    [LibraryImport("quickjs")]
    public static partial int JS_EnqueueJob(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, int, JSValue*, JSValue> job_func,
        int argc,
        JSValue* argv
    );

    [LibraryImport("quickjs")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool JS_IsJobPending(JSRuntime* rt);

    [LibraryImport("quickjs")]
    public static partial int JS_ExecutePendingJob(JSRuntime* rt, JSContext** pctx);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSSABTab
    {
        public byte** tab;
        public nuint len;
    }

    public const int JS_WRITE_OBJ_BYTECODE = (1 << 0);
    public const int JS_WRITE_OBJ_BSWAP = 0;
    public const int JS_WRITE_OBJ_SAB = (1 << 2);
    public const int JS_WRITE_OBJ_REFERENCE = (1 << 3);
    public const int JS_WRITE_OBJ_STRIP_SOURCE = (1 << 4);
    public const int JS_WRITE_OBJ_STRIP_DEBUG = (1 << 5);

    [LibraryImport("quickjs")]
    public static partial byte* JS_WriteObject(JSContext* ctx, nuint* psize, JSValue obj, int flags);

    [LibraryImport("quickjs")]
    public static partial byte* JS_WriteObject2(
        JSContext* ctx,
        nuint* psize,
        JSValue obj,
        int flags,
        JSSABTab* psab_tab
    );

    public const int JS_READ_OBJ_BYTECODE = (1 << 0);
    public const int JS_READ_OBJ_ROM_DATA = 0;
    public const int JS_READ_OBJ_SAB = (1 << 2);
    public const int JS_READ_OBJ_REFERENCE = (1 << 3);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ReadObject(JSContext* ctx, byte* buf, nuint buf_len, int flags);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_ReadObject2(
        JSContext* ctx,
        byte* buf,
        nuint buf_len,
        int flags,
        JSSABTab* psab_tab
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_EvalFunction(JSContext* ctx, JSValue fun_obj);

    [LibraryImport("quickjs")]
    public static partial int JS_ResolveModule(JSContext* ctx, JSValue obj);

    [LibraryImport("quickjs")]
    public static partial JSAtom JS_GetScriptOrModuleName(JSContext* ctx, int n_stack_levels);

    [LibraryImport("quickjs")]
    public static partial JSValue JS_LoadModule(JSContext* ctx, byte* basename, byte* filename);

    public enum JSCFunctionEnum
    {
        JS_CFUNC_generic,
        JS_CFUNC_generic_magic,
        JS_CFUNC_constructor,
        JS_CFUNC_constructor_magic,
        JS_CFUNC_constructor_or_func,
        JS_CFUNC_constructor_or_func_magic,
        JS_CFUNC_f_f,
        JS_CFUNC_f_f_f,
        JS_CFUNC_getter,
        JS_CFUNC_setter,
        JS_CFUNC_getter_magic,
        JS_CFUNC_setter_magic,
        JS_CFUNC_iterator_next,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct JSCFunctionType
    {
        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> generic;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, JSValue> generic_magic;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> constructor;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, JSValue> constructor_magic;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> constructor_or_func;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<double, double> f_f;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<double, double, double> f_f_f;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSValue> getter;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSValue, JSValue> setter;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue> getter_magic;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, JSValue, int, JSValue> setter_magic;

        [FieldOffset(0)]
        public delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int*, int, JSValue> iterator_next;
    }

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewCFunction2(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> func,
        byte* name,
        int length,
        JSCFunctionEnum cproto,
        int magic
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewCFunction3(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> func,
        byte* name,
        int length,
        JSCFunctionEnum cproto,
        int magic,
        JSValue proto_val
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewCFunctionData(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, JSValue*, JSValue> func,
        int length,
        int magic,
        int data_len,
        JSValue* data
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewCFunctionData2(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, JSValue*, JSValue> func,
        byte* name,
        int length,
        int magic,
        int data_len,
        JSValue* data
    );

    [LibraryImport("quickjs")]
    public static partial JSValue JS_NewCClosure(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, void*, JSValue> func,
        byte* name,
        delegate* unmanaged[Cdecl]<void*, void> opaque_finalize,
        int length,
        int magic,
        void* opaque
    );

    public static JSValue JS_NewCFunction(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, JSValue> func,
        byte* name,
        int length
    )
    {
        return JS_NewCFunction2(ctx, func, name, length, JSCFunctionEnum.JS_CFUNC_generic, 0);
    }

    public static JSValue JS_NewCFunctionMagic(
        JSContext* ctx,
        delegate* unmanaged[Cdecl]<JSContext*, JSValue, int, JSValue*, int, JSValue> func,
        byte* name,
        int length,
        JSCFunctionEnum cproto,
        int magic
    )
    {
        JSCFunctionType ft = default;
        ft.generic_magic = func;
        return JS_NewCFunction2(ctx, ft.generic, name, length, cproto, magic);
    }

    [LibraryImport("quickjs")]
    public static partial void JS_SetConstructor(JSContext* ctx, JSValue func_obj, JSValue proto);

    [StructLayout(LayoutKind.Sequential)]
    public struct JSCFunctionListEntryFunc
    {
        public byte length;
        public byte cproto;
        public JSCFunctionType cfunc;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSCFunctionListEntryGetSet
    {
        public JSCFunctionType get;
        public JSCFunctionType set;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSCFunctionListEntryAlias
    {
        public byte* name;
        public int base_idx;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSCFunctionListEntryPropList
    {
        public JSCFunctionListEntry* tab;
        public int len;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct JSCFunctionListEntryUnion
    {
        [FieldOffset(0)]
        public JSCFunctionListEntryFunc func;

        [FieldOffset(0)]
        public JSCFunctionListEntryGetSet getset;

        [FieldOffset(0)]
        public JSCFunctionListEntryAlias alias;

        [FieldOffset(0)]
        public JSCFunctionListEntryPropList prop_list;

        [FieldOffset(0)]
        public byte* str;

        [FieldOffset(0)]
        public int i32;

        [FieldOffset(0)]
        public long i64;

        [FieldOffset(0)]
        public ulong u64;

        [FieldOffset(0)]
        public double f64;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSCFunctionListEntry
    {
        public byte* name;
        public byte prop_flags;
        public byte def_type;
        public short magic;
        public JSCFunctionListEntryUnion u;
    }

    public const int JS_DEF_CFUNC = 0;
    public const int JS_DEF_CGETSET = 1;
    public const int JS_DEF_CGETSET_MAGIC = 2;
    public const int JS_DEF_PROP_STRING = 3;
    public const int JS_DEF_PROP_INT32 = 4;
    public const int JS_DEF_PROP_INT64 = 5;
    public const int JS_DEF_PROP_DOUBLE = 6;
    public const int JS_DEF_PROP_UNDEFINED = 7;
    public const int JS_DEF_OBJECT = 8;
    public const int JS_DEF_ALIAS = 9;

    [LibraryImport("quickjs")]
    public static partial int JS_SetPropertyFunctionList(
        JSContext* ctx,
        JSValue obj,
        JSCFunctionListEntry* tab,
        int len
    );

    [LibraryImport("quickjs")]
    public static partial JSModuleDef* JS_NewCModule(
        JSContext* ctx,
        byte* name_str,
        delegate* unmanaged[Cdecl]<JSContext*, JSModuleDef*, int> func
    );

    [LibraryImport("quickjs")]
    public static partial int JS_AddModuleExport(JSContext* ctx, JSModuleDef* m, byte* name_str);

    [LibraryImport("quickjs")]
    public static partial int JS_AddModuleExportList(
        JSContext* ctx,
        JSModuleDef* m,
        JSCFunctionListEntry* tab,
        int len
    );

    [LibraryImport("quickjs")]
    public static partial int JS_SetModuleExport(JSContext* ctx, JSModuleDef* m, byte* export_name, JSValue val);

    [LibraryImport("quickjs")]
    public static partial int JS_SetModuleExportList(
        JSContext* ctx,
        JSModuleDef* m,
        JSCFunctionListEntry* tab,
        int len
    );

    /* Version */
    public const int QJS_VERSION_MAJOR = 0;
    public const int QJS_VERSION_MINOR = 12;
    public const int QJS_VERSION_PATCH = 1;
    public const string QJS_VERSION_SUFFIX = "";

    [LibraryImport("quickjs")]
    public static partial byte* JS_GetVersion();
}
