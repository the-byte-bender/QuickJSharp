namespace QuickJSharp;

/// <summary>
/// Provides a way to extend the functionality of a <see cref="JSRuntime"/> and its associated <see cref="JSContext"/>s.
/// </summary>
/// <remarks>
/// Extensions are useful for modularizing complex logic and setting up globals across contexts.
/// </remarks>
public interface IRuntimeExtension : IDisposable
{
    /// <summary>
    /// Initializes the extension for a specific <see cref="JSRuntime"/>.
    /// </summary>
    /// <param name="runtime">The runtime instance this extension is being attached to.</param>
    /// <remarks>
    /// This is called when the extension is first added to the runtime. 
    /// Use this to register class definitions or runtime-wide configuration.
    /// <para>
    /// Exceptions thrown here will be forwarded and will prevent the extension from being added to the runtime.
    /// </para>
    /// </remarks>
    void Initialize(JSRuntime runtime);

    /// <summary>
    /// Sets up a newly created <see cref="JSContext"/>.
    /// </summary>
    /// <param name="context">The context to configure.</param>
    /// <remarks>
    /// This is called for every new context created within the runtime.
    /// Use this to inject stuff to the global scope.
    /// <para>
    /// Exceptions thrown here will be forwarded and the context will fail to initialize.
    /// </para>
    /// </remarks>
    void SetupContext(JSContext context);
}
