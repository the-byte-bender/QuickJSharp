namespace QuickJSharp.Bindings;

/// <summary>
/// A binding that exposes a C# class to JavaScript as a constructor function with a prototype chain.
/// </summary>
/// <remarks>
/// This class is primarily intended for use by the QuickJSharp source generator, which emits a concrete <see cref="ClassBinding"/>.
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
}
