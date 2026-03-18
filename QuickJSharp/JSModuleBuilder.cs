namespace QuickJSharp;

/// <summary>
/// A helper builder to simplify creating QuickJS native modules.
/// </summary>
/// <remarks>
/// This handles the two-stage ESM registration (metadata phase and initialization phase)
/// automatically from a single list of definitions.
/// </remarks>
public sealed class JSModuleBuilder
{
    private readonly JSContext _ctx;
    private readonly string _name;
    private readonly List<ModuleEntry> _entries = [];

    private readonly record struct ModuleEntry(string Name, Func<JSContext, JSValue> Factory);

    internal JSModuleBuilder(JSContext ctx, string name)
    {
        _ctx = ctx;
        _name = name;
    }

    /// <summary>
    /// Adds an export to the module with a factory to create its value.
    /// </summary>
    /// <remarks>
    /// The factory lambda will only be called when the module is actually
    /// evaluated (initialized) by the QuickJS engine.
    /// </remarks>
    /// <param name="name">The name of the export.</param>
    /// <param name="factory">A function that creates the export value given the current context.</param>
    /// <returns>This builder instance.</returns>
    public JSModuleBuilder Export(string name, Func<JSContext, JSValue> factory)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Export name cannot be null or empty.", nameof(name));

        _entries.Add(new ModuleEntry(name, factory));
        return this;
    }

    /// <summary>
    /// Builds the JSModule and returns it.
    /// </summary>
    /// <remarks>
    /// This should be called and returned within a <see cref="JSRuntime.ModuleLoader"/> delegate.
    /// It automatically registers the metadata and sets up the initialization callback.
    /// </remarks>
    public JSModule Build()
    {
        var entries = _entries;
        var module = _ctx.NewModule(
            _name,
            (ctx, mod) =>
            {
                foreach (var entry in entries)
                {
                    JSValue val = entry.Factory(ctx);
                    mod.SetExport(entry.Name, val);
                }
                return 0; // Success
            }
        );

        if (module.HasValue)
        {
            foreach (var entry in entries)
            {
                module.Value.AddExport(entry.Name);
            }
        }
        else
        {
            throw new InvalidOperationException($"Failed to create module '{_name}'");
        }

        return module.Value;
    }
}
