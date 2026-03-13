using QuickJSharp;

namespace QuickJSharp.Tests;

public class ModuleTests : IDisposable
{
    private readonly JSRuntime _rt;
    private readonly JSContext _ctx;

    public ModuleTests()
    {
        _rt = new JSRuntime();
        _ctx = _rt.CreateContext();
    }

    [Fact]
    public void Can_Create_Native_Module()
    {
        _rt.ModuleLoader = (ctx, name) =>
        {
            if (name == "test")
            {
                return ctx.CreateModule("test")
                    .Export("magic", c => c.NewInt32(42))
                    .Build();
            }
            return null;
        };

        var val = _ctx.Eval("import { magic } from 'test'; globalThis.result = magic;", "main.js", JSEvalFlags.Module);

        Assert.False(val.IsException, _ctx.HasException ? _ctx.GetException().ToString(_ctx) : "No exception");

        var result = _ctx.GlobalObject.GetProperty(_ctx, "result");
        Assert.Equal(42, result.ToInt32(_ctx));
    }

    [Fact]
    public void Native_Module_Supports_Functions()
    {
        bool called = false;

        _rt.ModuleLoader = (ctx, name) =>
        {
            if (name == "logger")
            {
                return ctx.CreateModule("logger")
                    .Export("log", c => c.NewFunction((cx, self, args) =>
                    {
                        called = true;
                        return cx.Undefined;
                    }, "log"))
                    .Build();
            }
            return null;
        };

        _ctx.Eval("import { log } from 'logger'; log();", "main.js", JSEvalFlags.Module);

        Assert.True(called);
    }

    [Fact]
    public void Module_Loader_Handles_Errors()
    {
        _rt.ModuleLoader = (ctx, name) =>
        {
            throw new Exception("Custom loader error");
        };

        var val = _ctx.Eval("import 'nothing'", "main.js", JSEvalFlags.Module);
        Assert.True(val.IsException);

        var ex = _ctx.GetException();
        Assert.Contains("Custom loader error", ex.ToString(_ctx));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
