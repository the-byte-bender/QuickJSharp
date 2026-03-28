using System.Collections.Generic;

namespace QuickJSharp.Bindings;

/// <summary>
/// A binding that exposes a C# enum to JavaScript as a global object containing bidirectional mappings.
/// </summary>
public abstract class EnumBinding : BindingBase
{
    private readonly string _jsName;
    private readonly (string Name, long Value)[] _members;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumBinding"/> class.
    /// </summary>
    /// <param name="registry">The registry this binding belongs to.</param>
    /// <param name="jsName">The name of the enum in JavaScript.</param>
    /// <param name="members">The members of the enum, paired with their underlying numeric values.</param>
    protected EnumBinding(BindingsRegistry registry, string jsName, (string Name, long Value)[] members)
        : base(registry)
    {
        _jsName = jsName;
        _members = members;
    }

    /// <inheritdoc/>
    public override void OnRuntime(JSRuntime runtime) { }

    /// <inheritdoc/>
    public override void OnContext(JSContext context)
    {
        var global = context.GlobalObject;
        var enumObj = context.NewObject();

        const JSPropertyFlags flags = JSPropertyFlags.Enumerable;

        foreach (var (name, value) in _members)
        {
            var jsName = context.NewString(name);
            var jsValue = context.NewInt64(value);

            // Enum.Member = Value
            enumObj.DefineProperty(context, name, jsValue, flags);

            // Enum[Value] = "Member"
            enumObj.DefineProperty(context, value.ToString(), jsName, flags);
        }

        global.SetProperty(context, _jsName, enumObj);
        global.Free(context);
    }

    /// <inheritdoc/>
    public override void Dispose() { }
}
