using QuickJSharp;
using QuickJSharp.Bindings;

namespace QuickJSharp.Demo;

[JSExpose]
public enum DemoEnum
{
    FirstValue,
    SecondValue,

    [JSName("aliased_value")]
    ThirdValue,
}

[JSBindingsRegistry]
[JSNamingConvention(EnumMembers = NamingPreference.ScreamingSnakeCase)]
public partial class DemoRegistry : BindingsRegistry
{
    public partial DemoRegistry();
}

[JSBindingsRegistry]
public partial class AnotherRegistry : BindingsRegistry
{
    public partial AnotherRegistry();
}

class Program
{
    static void Main(string[] args)
    {
        using var runtime = new JSRuntime();
        runtime.AddExtension(new DemoRegistry());
        using var context = runtime.CreateContext();

        var result = context.Eval(
            "DemoEnum.FIRST_VALUE=== 0 && DemoEnum[1] === 'SECOND_VALUE' && DemoEnum.aliased_value === 2"
        );
        Console.WriteLine($"Enum Verification Result: {result.ToString(context)}");

        result.Free(context);
    }
}
