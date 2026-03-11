using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// Flags for evaluating Javascript code.
/// </summary>
[Flags]
public enum JSEvalFlags
{
    /// <summary>
    /// Global code (default). The script is evaluated in the global scope.
    /// </summary>
    Global = QuickJS.JS_EVAL_TYPE_GLOBAL,

    /// <summary>
    /// Module code. The script is evaluated as a module. 
    /// Imports and exports are allowed.
    /// </summary>
    Module = QuickJS.JS_EVAL_TYPE_MODULE,

    /// <summary>
    /// Force 'strict' mode for the evaluation, even if not specified in the script.
    /// </summary>
    Strict = QuickJS.JS_EVAL_FLAG_STRICT,

    /// <summary>
    /// Compile the script but do not execute it. 
    /// The returned <see cref="JSValue"/> has the tag <see cref="QuickJS.JSTag.FUNCTION_BYTECODE"/> 
    /// or <see cref="QuickJS.JSTag.MODULE"/>. It can be executed later using <see cref="JSContext.EvalFunction"/>.
    /// </summary>
    CompileOnly = QuickJS.JS_EVAL_FLAG_COMPILE_ONLY,

    /// <summary>
    /// Don't include the stack frames before this eval in the Error() backtraces.
    /// Useful for hiding host-side wrapper frames from Javascript stack traces.
    /// </summary>
    BacktraceBarrier = QuickJS.JS_EVAL_FLAG_BACKTRACE_BARRIER,

    /// <summary>
    /// Allow top-level await in normal scripts. 
    /// When used, <see cref="JSContext.Eval"/> returns a Promise if top-level await is encountered.
    /// Only compatible with <see cref="Global"/>.
    /// </summary>
    Async = QuickJS.JS_EVAL_FLAG_ASYNC,
}

/// <summary>
/// Flags for writing/serializing Javascript objects to bytecode.
/// </summary>
[Flags]
public enum JSWriteObjectFlags
{
    /// <summary>
    /// Allow writing bytecode (functions/modules). 
    /// Must be set to serialize executable code.
    /// </summary>
    Bytecode = QuickJS.JS_WRITE_OBJ_BYTECODE,

    /// <summary>
    /// Allow the serialization of SharedArrayBuffers.
    /// </summary>
    SharedArrayBuffer = QuickJS.JS_WRITE_OBJ_SAB,

    /// <summary>
    /// Allow object references. This enables the serialization of circular 
    /// or shared object graphs by assigning IDs to objects.
    /// </summary>
    Reference = QuickJS.JS_WRITE_OBJ_REFERENCE,

    /// <summary>
    /// Strip the source code associated with functions in the resulting bytecode.
    /// Reduces the size of the output but removes source information from stack traces.
    /// </summary>
    StripSource = QuickJS.JS_WRITE_OBJ_STRIP_SOURCE,

    /// <summary>
    /// Strip debug information (like line numbers) from the resulting bytecode.
    /// Significant size reduction at the cost of debugging capabilities.
    /// </summary>
    StripDebug = QuickJS.JS_WRITE_OBJ_STRIP_DEBUG,
}

/// <summary>
/// Flags for reading/deserializing Javascript objects from bytecode.
/// </summary>
[Flags]
public enum JSReadObjectFlags
{
    /// <summary>
    /// Allow reading bytecode (functions/modules). 
    /// Must match the setting used during serialization.
    /// </summary>
    Bytecode = QuickJS.JS_READ_OBJ_BYTECODE,

    /// <summary>
    /// The input buffer is assumed to be in ROM and its contents will not be copied.
    /// The buffer must remain valid for the lifetime of the created objects.
    /// </summary>
    RomData = QuickJS.JS_READ_OBJ_ROM_DATA,

    /// <summary>
    /// Allow the deserialization of SharedArrayBuffers.
    /// </summary>
    SharedArrayBuffer = QuickJS.JS_READ_OBJ_SAB,

    /// <summary>
    /// Allow object references. Required if the bytecode was written 
    /// with <see cref="JSWriteObjectFlags.Reference"/>.
    /// </summary>
    Reference = QuickJS.JS_READ_OBJ_REFERENCE,
}

/// <summary>
/// Flags for property enumeration used by <see cref="QuickJS.JS_GetOwnPropertyNames"/>.
/// </summary>
[Flags]
public enum JSPropertyFlags
{
    /// <summary>
    /// Include string-keyed properties.
    /// </summary>
    StringMask = QuickJS.JS_GPN_STRING_MASK,

    /// <summary>
    /// Include symbol-keyed properties.
    /// </summary>
    SymbolMask = QuickJS.JS_GPN_SYMBOL_MASK,

    /// <summary>
    /// Include private-keyed properties.
    /// </summary>
    PrivateMask = QuickJS.JS_GPN_PRIVATE_MASK,

    /// <summary>
    /// Only include enumerable properties.
    /// </summary>
    EnumOnly = QuickJS.JS_GPN_ENUM_ONLY,

    /// <summary>
    /// Set the enumerable bit for the returned property names.
    /// </summary>
    SetEnum = QuickJS.JS_GPN_SET_ENUM,
}

