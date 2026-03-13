namespace QuickJSharp.Tests;

public class StabilityTests : IDisposable
{
    private readonly JSRuntime _rt;
    private readonly JSContext _ctx;

    public StabilityTests()
    {
        _rt = new JSRuntime();
        _ctx = _rt.CreateContext();
    }

    [Fact]
    public void Pinning_Survives_GC_During_Callback()
    {
        bool resultValid = false;
        var func = _ctx.NewFunction((ctx, thisVal, args) =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            resultValid = true;
            return ctx.NewInt32(100);
        }, "gcFunc");

        var result = func.Call(_ctx, []);

        Assert.True(resultValid);
        Assert.Equal(100, result.ToInt32(_ctx));
    }

    [Fact]
    public void Marshalling_Handles_Complex_UTF8()
    {
        string input = "Hello QuickJS! 🚀 ⚡ 日本語";

        var jsStr = _ctx.NewString(input);
        string? output = jsStr.ToString(_ctx);

        Assert.Equal(input, output);
    }

    [Fact]
    public async Task Runtimes_Are_Isolated_Across_Threads()
    {
        var t1 = Task.Run(() =>
        {
            using var rt = new JSRuntime();
            using var ctx = rt.CreateContext();
            ctx.GlobalObject.SetProperty(ctx, "id", ctx.NewInt32(1));
            return ctx.Eval("id").ToInt32(ctx);
        });

        var t2 = Task.Run(() =>
        {
            using var rt = new JSRuntime();
            using var ctx = rt.CreateContext();
            ctx.GlobalObject.SetProperty(ctx, "id", ctx.NewInt32(2));
            return ctx.Eval("id").ToInt32(ctx);
        });

        var res1 = await t1;
        var res2 = await t2;

        Assert.Equal(1, res1);
        Assert.Equal(2, res2);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
