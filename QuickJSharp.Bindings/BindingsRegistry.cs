using System.Runtime.CompilerServices;

namespace QuickJSharp.Bindings;

/// <summary>
/// An <see cref="IRuntimeExtension"/> that applies a collection of
/// <see cref="BindingBase"/> instances to a <see cref="JSRuntime"/> and its
/// contexts
/// </summary>
/// <seealso cref="JSBindingsRegistryAttribute"/>
public abstract class BindingsRegistry : IRuntimeExtension
{
    private IReadOnlyList<BindingBase> _bindings = [];
    private Dictionary<Type, ClassBinding> _classBindingMap = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingsRegistry"/> class.
    /// </summary>
    protected BindingsRegistry(IEnumerable<Func<BindingsRegistry, BindingBase>> factories)
    {
        _bindings = Sort(factories.Select(f => f(this)).ToList());
    }

    /// <summary>
    /// Registers a binding for the specified CLR type. This is used by the source generator to associate bindings with the top level types they represent.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterForClass<T>(ClassBinding binding)
    {
        _classBindingMap.Add(typeof(T), binding);
    }

    /// <summary>
    /// Gets the binding for the specified CLR type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClassBinding? GetBindingFor<T>()
    {
        return _classBindingMap.GetValueOrDefault(typeof(T));
    }

    /// <inheritdoc/>
    public void Initialize(JSRuntime runtime)
    {
        foreach (var binding in _bindings)
        {
            binding.OnRuntime(runtime);
        }
    }

    /// <inheritdoc/>
    public void SetupContext(JSContext context)
    {
        foreach (var binding in _bindings)
        {
            binding.OnContext(context);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var binding in _bindings)
        {
            binding.Dispose();
        }
    }

    static IReadOnlyList<BindingBase> Sort(List<BindingBase> bindings)
    {
        var classBindings = bindings.OfType<ClassBinding>().ToList();
        var otherBindings = bindings.Where(b => b is not ClassBinding).ToList();
        var sorted = new List<BindingBase>(bindings.Count);
        var visited = new HashSet<Type>();

        void Visit(ClassBinding binding)
        {
            if (!visited.Add(binding.ClrType))
                return;
            if (binding.ParentType is { } parentType)
            {
                var parent =
                    classBindings.FirstOrDefault(b => b.ClrType == parentType)
                    ?? throw new InvalidOperationException(
                        $"No binding registered for parent type '{parentType.FullName}' "
                            + $"required by '{binding.ClrType.FullName}'."
                    );
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
