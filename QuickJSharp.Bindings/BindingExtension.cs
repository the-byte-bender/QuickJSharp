namespace QuickJSharp.Bindings;

/// <summary>
/// An <see cref="IRuntimeExtension"/> that applies a collection of
/// <see cref="BindingBase"/> instances to a <see cref="JSRuntime"/> and its
/// contexts
/// </summary>
/// <seealso cref="JSBindingsRegistryAttribute"/>
public abstract class BindingExtension : IRuntimeExtension
{
    private readonly IReadOnlyList<BindingBase> _bindings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingExtension"/> class.
    /// </summary>
    /// <param name="bindings">The bindings to include in this extension.</param>
    protected BindingExtension(params BindingBase[] bindings)
    {
        _bindings = Sort([.. bindings]);
    }

    /// <inheritdoc/>
    public void Initialize(JSRuntime runtime)
    {
        foreach (var binding in _bindings)
            binding.OnRuntime(runtime);
    }

    /// <inheritdoc/>
    public void SetupContext(JSContext context)
    {
        foreach (var binding in _bindings)
            binding.OnContext(context);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var binding in _bindings)
            binding.Dispose();
    }

    static IReadOnlyList<BindingBase> Sort(List<BindingBase> bindings)
    {
        var classBindings = bindings.OfType<ClassBinding>().ToList();
        var otherBindings = bindings.Where(b => b is not ClassBinding).ToList();
        var sorted = new List<BindingBase>(bindings.Count);
        var visited = new HashSet<Type>();

        void Visit(ClassBinding binding)
        {
            if (!visited.Add(binding.ClrType)) return;
            if (binding.ParentType is { } parentType)
            {
                var parent = classBindings.FirstOrDefault(b => b.ClrType == parentType)
                    ?? throw new InvalidOperationException(
                        $"No binding registered for parent type '{parentType.FullName}' " +
                        $"required by '{binding.ClrType.FullName}'.");
                Visit(parent);
            }
            sorted.Add(binding);
        }

        foreach (var binding in classBindings)
            Visit(binding);

        sorted.AddRange(otherBindings);
        return sorted;
    }
}
