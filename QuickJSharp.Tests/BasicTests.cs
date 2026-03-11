using QuickJSharp;

namespace QuickJSharp.Tests;

public class BasicTests : IDisposable
{
    private readonly JSRuntime _rt;
    private readonly JSContext _ctx;

    public BasicTests()
    {
        _rt = new JSRuntime();
        _ctx = _rt.CreateContext();
    }

    [Fact]
    public void Can_Create_Context()
    {
        Assert.NotNull(_ctx);
    }

    [Fact]
    public void Can_Evaluate_Simple_Expression()
    {
        var val = _ctx.Eval("1 + 2");
        Assert.True(val.IsNumber);
        Assert.Equal(3, val.ToInt32(_ctx));
    }

    [Fact]
    public void Can_Define_Global_Variable()
    {
        var global = _ctx.GlobalObject;
        global.SetProperty(_ctx, "myVar", _ctx.NewInt32(42));

        var val = _ctx.Eval("myVar");
        Assert.Equal(42, val.ToInt32(_ctx));
    }

    [Fact]
    public void Can_Handle_Strings()
    {
        string input = "Hello QuickJS!";
        var jsStr = _ctx.NewString(input);
        Assert.True(jsStr.IsString);
        Assert.Equal(input, jsStr.ToString(_ctx));
    }

    [Fact]
    public void Can_Catch_Exceptions()
    {
        var val = _ctx.Eval("throw new Error('test')");
        Assert.True(val.IsException);

        var ex = _ctx.GetException();
        Assert.True(ex.IsError);
        Assert.Contains("test", ex.ToString(_ctx));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
