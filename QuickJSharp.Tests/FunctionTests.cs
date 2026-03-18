namespace QuickJSharp.Tests;

public class FunctionTests : IDisposable
{
    private readonly JSRuntime _rt;
    private readonly JSContext _ctx;

    public FunctionTests()
    {
        _rt = new JSRuntime();
        _ctx = _rt.CreateContext();
    }

    [Fact]
    public void Can_Call_Managed_Function_From_JS()
    {
        bool called = false;
        var func = _ctx.NewFunction(
            (ctx, thisVal, args) =>
            {
                called = true;
                return ctx.NewInt32(666);
            },
            "myFunc"
        );

        _ctx.GlobalObject.SetProperty(_ctx, "myFunc", func);

        var result = _ctx.Eval("myFunc()");

        Assert.True(called);
        Assert.Equal(666, result.ToInt32(_ctx));
    }

    [Fact]
    public void Function_Can_Access_Arguments_From_JS()
    {
        var func = _ctx.NewFunction(
            (ctx, thisVal, args) =>
            {
                int sum = 0;
                foreach (var arg in args)
                {
                    sum += arg.ToInt32(ctx);
                }
                return ctx.NewInt32(sum);
            },
            "sum"
        );

        _ctx.GlobalObject.SetProperty(_ctx, "sum", func);

        var result = _ctx.Eval("sum(10, 20, 5)");

        Assert.Equal(35, result.ToInt32(_ctx));
    }

    [Fact]
    public void Function_Can_Access_This_From_JS()
    {
        var func = _ctx.NewFunction(
            (ctx, thisVal, args) =>
            {
                var x = thisVal.GetProperty(ctx, "x");
                return ctx.NewInt32(x.ToInt32(ctx) * 2);
            },
            "doubleX"
        );

        _ctx.Eval("globalThis.obj = { x: 21 };");
        var obj = _ctx.GlobalObject.GetProperty(_ctx, "obj");
        obj.SetProperty(_ctx, "doubleX", func);

        var result = _ctx.Eval("obj.doubleX()");

        Assert.Equal(42, result.ToInt32(_ctx));
    }

    [Fact]
    public void Function_Can_Throw_Exceptions()
    {
        var func = _ctx.NewFunction(
            (ctx, thisVal, args) =>
            {
                return ctx.ThrowTypeError("invalid argument count");
            },
            "failure"
        );

        var result = func.Call(_ctx, []);
        Assert.True(result.IsException);

        var ex = _ctx.GetException();
        Assert.Contains("invalid argument count", ex.ToString(_ctx));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
