#:project ./QuickJSharp/QuickJSharp.csproj
#:property AllowUnsafeBlocks=true
// Run in release (dotnet run -c Release Demo.cs)
// Or publish as AOT and run the resulting executable (dotnet publish -c Release Demo.cs) for more realistic and stable runs.
// This is a quickly thrown together demo of various QuickJSharp features and performance characteristics, not meant to be a formal benchmark or test suite. The code is intentionally straightforward and unoptimized to show typical usage patterns and relative overheads of different interop approaches.

using System.Runtime.InteropServices;
using System.Text;
using QuickJSharp;
using NativeJS = QuickJSharp.Native.QuickJS;

using JSRuntime rt = new();
using JSContext ctx = rt.CreateContext();

rt.ModuleLoader = (c, name) =>
{
    if (name == "calc")
    {
        return c.CreateModule(name)
            .Export("add", ctx => ctx.NewFunction((c, _, args) => c.NewDouble(args[0].ToDouble(c) + args[1].ToDouble(c)), "add"))
            .Export("sub", ctx => ctx.NewFunction((c, _, args) => c.NewDouble(args[0].ToDouble(c) - args[1].ToDouble(c)), "sub"))
            .Export("PI", ctx => ctx.NewBigInt64(3))
            .Build();
    }
    return null;
};

var sw = System.Diagnostics.Stopwatch.StartNew();
var timeObj = ctx.NewObject();
timeObj.SetProperty(ctx, "now", ctx.NewFunction((c, _, _) => c.NewDouble(sw.Elapsed.TotalMilliseconds), "now"));
ctx.GlobalObject.SetProperty(ctx, "Time", timeObj);

unsafe
{
    ctx.GlobalObject.SetProperty(ctx, "rawAdd", ctx.NewFunctionRaw(&Program.AddProxy, "rawAdd", 2));
}

int capturedIncrement = 1;
ctx.GlobalObject.SetProperty(ctx, "managedAdd", ctx.NewFunction((JSContext c, JSValue thisVal, ReadOnlySpan<JSValue> args) =>
{
    return c.NewInt32(args[0].ToInt32(c) + capturedIncrement + args[1].ToInt32(c) - 1);
}, "managedAdd"));

static JSValue AddManaged(JSContext c, JSValue thisVal, ReadOnlySpan<JSValue> args)
{
    return c.NewInt32(args[0].ToInt32(c) + args[1].ToInt32(c));
}
ctx.GlobalObject.SetProperty(ctx, "managedAddStatic", ctx.NewFunction(AddManaged, "managedAddStatic"));

const int Iterations = 1_000_000;

string benchScript = $@"
        (function() {{
            const iterations = {Iterations};
            const now = () => Time.now();
            
            let start = now();
            for(let i=0; i < iterations; i++) rawAdd(i, 1);
            let end = now();
            const rawTime = end - start;

            start = now();
            for(let i=0; i < iterations; i++) managedAdd(i, 1);
            end = now();
            const managedTime = end - start;

            start = now();
            for(let i=0; i < iterations; i++) managedAddStatic(i, 1);
            end = now();
            const managedStaticTime = end - start;

            return 'Iterations: ' + iterations.toLocaleString() + '\n' +
'-----------------------------------------\n' +
'RAW (Native Function*):  ' + rawTime + 'ms (' + (iterations / (rawTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'MANAGED (Lambda):         ' + managedTime + 'ms (' + (iterations / (managedTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'MANAGED (Static Method): ' + managedStaticTime + 'ms (' + (iterations / (managedStaticTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'Overhead (Lambda):       ' + (managedTime - rawTime).toFixed(2) + 'ms total extra cost';
        }})();";

JSValue resultWrap = ctx.Eval(benchScript);
Console.WriteLine(resultWrap.ToString(ctx));
resultWrap.Free(ctx);

string moduleDemoScript = @"
    import { add, sub, PI } from 'calc';
    console.log(`Add: ${add(10, 5)}`);
    console.log(`Sub: ${sub(10, 5)}`);
    console.log(`PI (integer): ${PI}`);
";
ctx.GlobalObject.SetProperty(ctx, "console", ctx.NewObject());
ctx.GlobalObject.GetProperty(ctx, "console").SetProperty(ctx, "log", ctx.NewFunction((c, _, args) =>
{
    Console.WriteLine(args[0].ToString(c));
    return c.Undefined;
}, "log"));

var modRes = ctx.Eval(moduleDemoScript, "main.js", JSEvalFlags.Module);
if (modRes.IsException)
{
    var err = ctx.GetException();
    Console.WriteLine($"Error: {err.ToString(ctx)}");
    err.Free(ctx);
}
modRes.Free(ctx);

public partial class Program
{
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static unsafe NativeJS.JSValue AddProxy(NativeJS.JSContext* ctx, NativeJS.JSValue this_val, int argc, NativeJS.JSValue* argv)
    {
        int a, b;
        NativeJS.JS_ToInt32(ctx, &a, argv[0]);
        NativeJS.JS_ToInt32(ctx, &b, argv[1]);
        return NativeJS.JS_NewInt32(ctx, a + b);
    }
}
