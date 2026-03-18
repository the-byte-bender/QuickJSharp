namespace QuickJSharp.Bindings;

/// <summary>
/// Base class for all QuickJSharp binding types.
/// </summary>
/// <remarks>
/// Bindings describe how a C# concept is exposed to JavaScript.
/// This class is primarily intended for use by the QuickJSharp source generator (<c>QuickJSharp.Bindings.Generators</c>), which emits concrete implementations
/// automatically from attributed classes. You can implement this directly if you
/// need to define bindings manually, but in most cases the generator is the
/// better path.
/// <para>
/// Each binding has two setup phases that mirror the QuickJS runtime/context
/// lifecycle. <see cref="OnRuntime"/> runs once when the binding is registered
/// with a <see cref="JSRuntime"/>. <see cref="OnContext"/> runs for every
/// <see cref="JSContext"/> created within that runtime.
/// </para>
/// </remarks>
public abstract class BindingBase : IDisposable
{
    /// <summary>
    /// Called once when this binding is registered with a <see cref="JSRuntime"/>.
    /// </summary>
    /// <param name="runtime">The runtime this binding is being registered with.</param>
    public abstract void OnRuntime(JSRuntime runtime);

    /// <summary>
    /// Called for every <see cref="JSContext"/> created within the associated <see cref="JSRuntime"/>.
    /// </summary>
    /// <param name="context">The context being configured.</param>
    public abstract void OnContext(JSContext context);

    /// <summary>
    /// Releases any native resources held by this binding
    /// </summary>
    public abstract void Dispose();
}
