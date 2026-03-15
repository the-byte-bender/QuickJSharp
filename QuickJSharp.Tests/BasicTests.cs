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

    [Fact]
    public void Can_Handle_Atoms()
    {
        var obj = _ctx.Eval("({ foo: 42, length: 1, if: 'keyword' })");

        var fooAtom = _ctx.NewAtom("foo");
        Assert.Equal(42, obj.GetProperty(_ctx, fooAtom).ToInt32(_ctx));

        var lengthAtom = _ctx.NewAtom("length");
        Assert.Equal(1, obj.GetProperty(_ctx, lengthAtom).ToInt32(_ctx));

        var ifAtom = _ctx.NewAtom("if");
        Assert.Equal("keyword", obj.GetProperty(_ctx, ifAtom).ToString(_ctx));

        var fooAtom2 = _ctx.NewAtom("foo");
        Assert.Equal(fooAtom.Value, fooAtom2.Value);

        fooAtom.Free(_ctx);
        fooAtom2.Free(_ctx);
        lengthAtom.Free(_ctx);
        ifAtom.Free(_ctx);
    }

    [Fact]
    public void Can_Handle_Symbols()
    {
        var symAtom = _ctx.NewSymbol("myUniqueSymbol");
        var obj = _ctx.Eval("({})");

        obj.SetProperty(_ctx, symAtom, _ctx.NewInt32(99));

        var val = obj.GetProperty(_ctx, symAtom);
        Assert.Equal(99, val.ToInt32(_ctx));

        // Verify it is NOT enumerable (standard symbol behavior)
        var keys = _ctx.Eval("(obj) => Object.keys(obj).length");
        var keyCount = keys.Call(_ctx, [obj]);
        Assert.Equal(0, keyCount.ToInt32(_ctx));

        symAtom.Free(_ctx);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
