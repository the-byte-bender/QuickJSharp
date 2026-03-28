using System.Runtime.CompilerServices;

namespace QuickJSharp.Bindings;

/// <summary>
/// A binding that exposes a C# class to JavaScript as a constructor function with a prototype chain.
/// </summary>
/// <remarks>
/// This class is primarily intended for use by the QuickJSharp source generator, which emits a concrete
/// <see cref="ClassBinding"/>.
/// <para>
/// Inheritance is handled automatically by the binding registry; if
/// <see cref="ParentType"/> is non-null, the registry ensures the parent binding's
/// prototype is set up before this binding's <see cref="BindingBase.OnRuntime"/>
/// is called.
/// </para>
/// </remarks>
public abstract class ClassBinding : BindingBase
{
    /// <summary>
    /// The CLR type this binding represents. Used as a unique identity key by the binding registry
    /// </summary>
    public abstract Type ClrType { get; }

    /// <summary>
    /// The CLR type of the parent class in the JavaScript prototype chain,
    /// or <c>null</c> if this class has no JS parent.
    /// </summary>
    /// <remarks>
    /// The parent type must itself have a registered <see cref="ClassBinding"/>.
    /// The registry will throw during registration if a non-null parent cannot be resolved.
    /// </remarks>
    public abstract Type? ParentType { get; }

    /// <summary>
    /// The JavaScript class ID.
    /// </summary>
    /// <remarks>
    /// Only valid after this is attached to a runtime.
    /// </remarks>
    public JSClassID ClassID { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassBinding"/> class with the specified registry.
    /// </summary>
    /// <param name="registry"></param>
    protected ClassBinding(BindingsRegistry registry)
        : base(registry) { }

    /// <summary>
    /// Gets the JavaScript prototype object for this class.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JSValue GetPrototype(JSContext ctx) => ctx.GetClassProto(ClassID);

    /// <summary>
    /// Unwraps a JavaScript object into an instance of the CLR type represented by this binding.
    /// </summary>
    public abstract Object? Unwrap(JSValue value);

    /// <summary>
    /// Wraps an instance of the CLR type represented by this binding into a JavaScript object.
    /// </summary>
    public abstract JSValue Wrap(JSContext ctx, Object? obj);
}
