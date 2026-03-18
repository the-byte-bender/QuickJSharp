#:project ./QuickJSharp/QuickJSharp.csproj
#:property AllowUnsafeBlocks=true
// Run in release (dotnet run -c Release Demo.cs)
// Or publish as AOT and run the resulting executable (dotnet publish -c Release Demo.cs) for more realisticstable runs.
// This is a quickly thrown together demo of various QuickJSharp features and performance characteristics,
// not meant to be a formal benchmark or test suite. The code is intentionally straightforward and unoptimized
// to show typical usage patterns and relative overheads of different interop approaches.
// Also we're using primitives here for simplicity. In real sinarios you'd be using strings and objects, which
// add overhead, especially for string marshalling, though we try to minimize that as much as possible but it's still
// something to be aware of in your usage and avoid where possible.

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
            .Export(
                "add",
                ctx => ctx.NewFunction((c, _, args) => c.NewDouble(args[0].ToDouble(c) + args[1].ToDouble(c)), "add")
            )
            .Export(
                "sub",
                ctx => ctx.NewFunction((c, _, args) => c.NewDouble(args[0].ToDouble(c) - args[1].ToDouble(c)), "sub")
            )
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
ctx.GlobalObject.SetProperty(
    ctx,
    "managedAdd",
    ctx.NewFunction(
        (JSContext c, JSValue thisVal, ReadOnlySpan<JSValue> args) =>
        {
            return c.NewInt32(args[0].ToInt32(c) + capturedIncrement + args[1].ToInt32(c) - 1);
        },
        "managedAdd"
    )
);

static JSValue AddManaged(JSContext c, JSValue thisVal, ReadOnlySpan<JSValue> args)
{
    return c.NewInt32(args[0].ToInt32(c) + args[1].ToInt32(c));
}
ctx.GlobalObject.SetProperty(ctx, "managedAddStatic", ctx.NewFunction(AddManaged, "managedAddStatic"));

var jsObj = ctx.Eval("({ get val() { return 42; } })");
ctx.GlobalObject.SetProperty(ctx, "jsObj", jsObj);

var nativeObj = ctx.NewObject();
var nativeKey = ctx.NewAtom("val");
var nativeGetter = ctx.NewFunction((c, _, _) => c.NewInt32(42), "getVal", 0);
nativeObj.DefineProperty(
    ctx,
    nativeKey,
    nativeGetter,
    JSValue.Undefined,
    JSPropertyFlags.Enumerable | JSPropertyFlags.Configurable
);
ctx.GlobalObject.SetProperty(ctx, "nativeObj", nativeObj);
nativeKey.Free(ctx);

const int Iterations = 1_000_000;

string benchScript =
    $@"
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

            start = now();
            for(let i=0; i < iterations; i++) {{ let x = jsObj.val; }}
            end = now();
            const jsGetterTime = end - start;

            start = now();
            for(let i=0; i < iterations; i++) {{ let x = nativeObj.val; }}
            end = now();
            const nativeGetterTime = end - start;

            return 'Iterations: ' + iterations.toLocaleString() + '\n' +
'-----------------------------------------\n' +
'FUNCTION CALLS:\n' +
'RAW (Native Function*):  ' + rawTime.toFixed(2) + 'ms (' + (iterations / (rawTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'MANAGED (Lambda):         ' + managedTime.toFixed(2) + 'ms (' + (iterations / (managedTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'MANAGED (Static Method): ' + managedStaticTime.toFixed(2) + 'ms (' + (iterations / (managedStaticTime || 1) * 1000).toFixed(0) + ' calls / sec)\n' +
'Overhead (Lambda):       ' + (managedTime - rawTime).toFixed(2) + 'ms total extra cost\n' +
'-----------------------------------------\n' +
'PROPERTY GETTERS:\n' +
'JS GETTER (get val()):    ' + jsGetterTime.toFixed(2) + 'ms (' + (iterations / (jsGetterTime || 1) * 1000).toFixed(0) + ' gets / sec)\n' +
'NATIVE GETTER (C# callback): ' + nativeGetterTime.toFixed(2) + 'ms (' + (iterations / (nativeGetterTime || 1) * 1000).toFixed(0) + ' gets / sec)\n' +
'-----------------------------------------\n' +
'Native vs JS overhead:   ' + (nativeGetterTime / (jsGetterTime || 1)).toFixed(2) + 'x';
        }})();";

JSValue resultWrap = ctx.Eval(benchScript);
Console.WriteLine(resultWrap.ToString(ctx));
resultWrap.Free(ctx);

string moduleDemoScript =
    @"
    import { add, sub, PI } from 'calc';
    console.log(`Add: ${add(10, 5)}`);
    console.log(`Sub: ${sub(10, 5)}`);
    console.log(`PI (integer): ${PI}`);
";
ctx.GlobalObject.SetProperty(ctx, "console", ctx.NewObject());
ctx.GlobalObject.GetProperty(ctx, "console")
    .SetProperty(
        ctx,
        "log",
        ctx.NewFunction(
            (c, _, args) =>
            {
                Console.WriteLine(args[0].ToString(c));
                return c.Undefined;
            },
            "log"
        )
    );

var modRes = ctx.Eval(moduleDemoScript, "main.js", JSEvalFlags.Module);
if (modRes.IsException)
{
    var err = ctx.GetException();
    Console.WriteLine($"Error: {err.ToString(ctx)}");
    modRes.Free(ctx);
}

Console.WriteLine("\n--- Property Access Benchmark (Atom vs String) ---");

var benchObj = ctx.Eval("({ propInt: 0, propStr: '' })");
try
{
    var propIntAtom = ctx.NewAtom("propInt");
    var propStrAtom = ctx.NewAtom("propStr");

    const int BenchIterations = 1_000_000;

    sw.Restart();
    for (int i = 0; i < BenchIterations; i++)
    {
        benchObj.SetProperty(ctx, "propInt", ctx.NewInt32(i));
    }
    var setIntStringTime = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < BenchIterations; i++)
    {
        benchObj.SetProperty(ctx, propIntAtom, ctx.NewInt32(i));
    }
    var setIntAtomTime = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    long sum = 0;
    for (int i = 0; i < BenchIterations; i++)
    {
        var val = benchObj.GetProperty(ctx, "propInt");
        sum += val.ToInt32(ctx);
        val.Free(ctx);
    }
    var getIntStringTime = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    sum = 0;
    for (int i = 0; i < BenchIterations; i++)
    {
        var val = benchObj.GetProperty(ctx, propIntAtom);
        sum += val.ToInt32(ctx);
        val.Free(ctx);
    }
    var getIntAtomTime = sw.Elapsed.TotalMilliseconds;

    var testStr = "Hello World";
    sw.Restart();
    for (int i = 0; i < BenchIterations; i++)
    {
        benchObj.SetProperty(ctx, "propStr", ctx.NewString(testStr));
    }
    var setStrStringTime = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    for (int i = 0; i < BenchIterations; i++)
    {
        benchObj.SetProperty(ctx, propStrAtom, ctx.NewString(testStr));
    }
    var setStrAtomTime = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"Iterations: {BenchIterations:N0}");
    Console.WriteLine(
        $"SET Int (Str Key): {setIntStringTime:F2}ms ({(BenchIterations / setIntStringTime * 1000):N0} ops/s)"
    );
    Console.WriteLine(
        $"SET Int (Atom Key): {setIntAtomTime:F2}ms ({(BenchIterations / setIntAtomTime * 1000):N0} ops/s)"
    );
    Console.WriteLine(
        $"GET Int (Str Key): {getIntStringTime:F2}ms ({(BenchIterations / getIntStringTime * 1000):N0} ops/s)"
    );
    Console.WriteLine(
        $"GET Int (Atom Key): {getIntAtomTime:F2}ms ({(BenchIterations / getIntAtomTime * 1000):N0} ops/s)"
    );
    Console.WriteLine(
        $"SET Str (Str Key): {setStrStringTime:F2}ms ({(BenchIterations / setStrStringTime * 1000):N0} ops/s)"
    );
    Console.WriteLine(
        $"SET Str (Atom Key): {setStrAtomTime:F2}ms ({(BenchIterations / setStrAtomTime * 1000):N0} ops/s)"
    );

    propIntAtom.Free(ctx);
    propStrAtom.Free(ctx);
}
finally
{
    benchObj.Free(ctx);
}

public partial class Program
{
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static unsafe NativeJS.JSValue AddProxy(
        NativeJS.JSContext* ctx,
        NativeJS.JSValue this_val,
        int argc,
        NativeJS.JSValue* argv
    )
    {
        int a,
            b;
        NativeJS.JS_ToInt32(ctx, &a, argv[0]);
        NativeJS.JS_ToInt32(ctx, &b, argv[1]);
        return NativeJS.JS_NewInt32(ctx, a + b);
    }
}
