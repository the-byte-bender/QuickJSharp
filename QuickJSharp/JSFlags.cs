using QuickJSharp.Native;

namespace QuickJSharp;

/// <summary>
/// Flags passed to a class 'call' method.
/// </summary>
[Flags]
public enum JSCallFlags
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// The function is called as a constructor (via 'new').
    /// </summary>
    Constructor = QuickJS.JS_CALL_FLAG_CONSTRUCTOR,
}

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
    /// The returned result is a bytecode object that can be executed later.
    /// </summary>
    CompileOnly = QuickJS.JS_EVAL_FLAG_COMPILE_ONLY,

    /// <summary>
    /// Don't include the stack frames before this eval in the Error() backtraces.
    /// </summary>
    BacktraceBarrier = QuickJS.JS_EVAL_FLAG_BACKTRACE_BARRIER,

    /// <summary>
    /// Allow top-level await in normal scripts.
    /// When used, evaluation returns a Promise if top-level await is encountered.
    /// Only compatible with <see cref="Global"/>. <see cref="Module"/> always allows top-level await.
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
/// Flags for dumping runtime information and internal diagnostics.
/// </summary>
/// <remarks>
/// QuickJS compilation happens in multiple passes:
/// <list type="bullet">
/// <item><description>Pass 1: Syntax analysis and basic instruction generation.</description></item>
/// <item><description>Pass 2: Variable resolution and stack usage computation.</description></item>
/// <item><description>Pass 3: Final peephole optimization and bytecode generation.</description></item>
/// </list>
/// </remarks>
[Flags]
public enum JSDumpFlags : ulong
{
    /// <summary>
    /// Dump the final bytecode after pass 3 optimizations.
    /// </summary>
    BytecodeFinal = QuickJS.JS_DUMP_BYTECODE_FINAL,

    /// <summary>
    /// Dump the intermediate bytecode after pass 2.
    /// </summary>
    BytecodePass2 = QuickJS.JS_DUMP_BYTECODE_PASS2,

    /// <summary>
    /// Dump the initial raw bytecode after pass 1.
    /// </summary>
    BytecodePass1 = QuickJS.JS_DUMP_BYTECODE_PASS1,

    /// <summary>
    /// Dump the bytecode in hexadecimal format.
    /// </summary>
    BytecodeHex = QuickJS.JS_DUMP_BYTECODE_HEX,

    /// <summary>
    /// Dump the Program Counter (PC) to line number mapping table.
    /// </summary>
    BytecodePc2Line = QuickJS.JS_DUMP_BYTECODE_PC2LINE,

    /// <summary>
    /// Dump computed stack sizes and frame layouts.
    /// </summary>
    BytecodeStack = QuickJS.JS_DUMP_BYTECODE_STACK,

    /// <summary>
    /// Dump each instruction to console as it is executed (Interpreter trace).
    /// </summary>
    BytecodeStep = QuickJS.JS_DUMP_BYTECODE_STEP,

    /// <summary>
    /// Dump the deserialized objects during loading.
    /// </summary>
    ReadObject = QuickJS.JS_DUMP_READ_OBJECT,

    /// <summary>
    /// Dump a trace of all managed memory releases.
    /// </summary>
    Free = QuickJS.JS_DUMP_FREE,

    /// <summary>
    /// Dump a trace of Garbage Collection cycles.
    /// </summary>
    GC = QuickJS.JS_DUMP_GC,

    /// <summary>
    /// Dump a trace of memory specifically released by the GC.
    /// </summary>
    GCFree = QuickJS.JS_DUMP_GC_FREE,

    /// <summary>
    /// Trace the steps of the module resolver.
    /// </summary>
    ModuleResolve = QuickJS.JS_DUMP_MODULE_RESOLVE,

    /// <summary>
    /// Trace Promise creation, resolution, and microtask scheduling.
    /// </summary>
    Promise = QuickJS.JS_DUMP_PROMISE,

    /// <summary>
    /// Report memory leaks (objects/strings) upon runtime destruction.
    /// </summary>
    Leaks = QuickJS.JS_DUMP_LEAKS,

    /// <summary>
    /// Report leaked internal atoms upon runtime destruction.
    /// </summary>
    AtomLeaks = QuickJS.JS_DUMP_ATOM_LEAKS,

    /// <summary>
    /// Dump final memory allocation statistics.
    /// </summary>
    Mem = QuickJS.JS_DUMP_MEM,

    /// <summary>
    /// Dump the internal state of all remaining objects on cleanup.
    /// </summary>
    Objects = QuickJS.JS_DUMP_OBJECTS,

    /// <summary>
    /// Dump all remaining atoms on cleanup.
    /// </summary>
    Atoms = QuickJS.JS_DUMP_ATOMS,

    /// <summary>
    /// Dump all remaining shapes (hidden classes) on cleanup.
    /// </summary>
    Shapes = QuickJS.JS_DUMP_SHAPES,
}

/// <summary>
/// Flags for property descriptors and property definition.
/// </summary>
[Flags]
public enum JSPropertyFlags
{
    /// <summary>
    /// The property is configurable (can be deleted or changed).
    /// </summary>
    Configurable = QuickJS.JS_PROP_CONFIGURABLE,

    /// <summary>
    /// The property is writable (its value can be changed).
    /// </summary>
    Writable = QuickJS.JS_PROP_WRITABLE,

    /// <summary>
    /// The property is enumerable (appears in for-in loops).
    /// </summary>
    Enumerable = QuickJS.JS_PROP_ENUMERABLE,

    /// <summary>
    /// Shortcut for Configurable | Writable | Enumerable.
    /// </summary>
    CWEL = QuickJS.JS_PROP_C_W_E,

    /// <summary>
    /// Internal flag used by QuickJS to identify the magic 'length' property of Arrays.
    /// Defining custom properties with this flag is not supported and may cause instability.
    /// </summary>
    Length = QuickJS.JS_PROP_LENGTH,

    /// <summary>
    /// Normal property with absolute value.
    /// </summary>
    Normal = QuickJS.JS_PROP_NORMAL,

    /// <summary>
    /// Property with getter and/or setter.
    /// </summary>
    GetSet = QuickJS.JS_PROP_GETSET,

    /// <summary>
    /// Throw an exception if the operation fails.
    /// </summary>
    Throw = QuickJS.JS_PROP_THROW,

    /// <summary>
    /// Throw an exception if the operation fails in strict mode.
    /// </summary>
    ThrowStrict = QuickJS.JS_PROP_THROW_STRICT,

    /// <summary>
    /// Mask to indicate that the 'Configurable' attribute is provided in the descriptor.
    /// Without this flag, <c>JS_DefineProperty</c> will ignore the <see cref="Configurable"/> bit.
    /// </summary>
    HasConfigurable = QuickJS.JS_PROP_HAS_CONFIGURABLE,

    /// <summary>
    /// Mask to indicate that the 'Writable' attribute is provided in the descriptor.
    /// Without this flag, <c>JS_DefineProperty</c> will ignore the <see cref="Writable"/> bit.
    /// </summary>
    HasWritable = QuickJS.JS_PROP_HAS_WRITABLE,

    /// <summary>
    /// Mask to indicate that the 'Enumerable' attribute is provided in the descriptor.
    /// Without this flag, <c>JS_DefineProperty</c> will ignore the <see cref="Enumerable"/> bit.
    /// </summary>
    HasEnumerable = QuickJS.JS_PROP_HAS_ENUMERABLE,

    /// <summary>
    /// Mask to indicate that a getter function is provided in the descriptor.
    /// Required when setting accessor properties via <c>JS_DefineProperty</c>.
    /// </summary>
    HasGet = QuickJS.JS_PROP_HAS_GET,

    /// <summary>
    /// Mask to indicate that a setter function is provided in the descriptor.
    /// Required when setting accessor properties via <c>JS_DefineProperty</c>.
    /// </summary>
    HasSet = QuickJS.JS_PROP_HAS_SET,

    /// <summary>
    /// Mask to indicate that a value is provided in the descriptor.
    /// Required when defining data properties via <c>JS_DefineProperty</c>.
    /// </summary>
    HasValue = QuickJS.JS_PROP_HAS_VALUE,
}

/// <summary>
/// Flags for retrieving property names from an object.
/// </summary>
[Flags]
public enum JSGetPropertyNamesFlags
{
    /// <summary>
    /// Include string-keyed properties.
    /// </summary>
    String = QuickJS.JS_GPN_STRING_MASK,

    /// <summary>
    /// Include symbol-keyed properties.
    /// </summary>
    Symbol = QuickJS.JS_GPN_SYMBOL_MASK,

    /// <summary>
    /// Include private-keyed properties.
    /// </summary>
    Private = QuickJS.JS_GPN_PRIVATE_MASK,

    /// <summary>
    /// Only include enumerable properties.
    /// </summary>
    EnumOnly = QuickJS.JS_GPN_ENUM_ONLY,

    /// <summary>
    /// Set the enumerable flag in the result.
    /// </summary>
    SetEnum = QuickJS.JS_GPN_SET_ENUM,
}
