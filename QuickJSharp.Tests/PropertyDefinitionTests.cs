namespace QuickJSharp.Tests;

public class PropertyDefinitionTests : IDisposable
{
    private readonly JSRuntime _runtime;
    private readonly JSContext _ctx;

    public PropertyDefinitionTests()
    {
        _runtime = new JSRuntime();
        _ctx = _runtime.CreateContext();
    }

    [Fact]
    public void TestNonEnumerableProperty()
    {
        var obj = _ctx.NewObject();
        var key = _ctx.NewAtom("hidden");
        var val = _ctx.NewInt32(42);

        obj.DefineProperty(_ctx, key, val, JSPropertyFlags.Configurable | JSPropertyFlags.Writable);

        var retrieved = obj.GetProperty(_ctx, key);
        Assert.Equal(42, retrieved.ToInt32(_ctx));

        // Should NOT be enumerable.
        var global = _ctx.GlobalObject;
        var objectKeys = global.GetProperty(_ctx, "Object").GetProperty(_ctx, "keys");
        var keys = objectKeys.Call(_ctx, [obj]);

        Assert.True(keys.IsArray);
        Assert.Equal(0, keys.GetProperty(_ctx, "length").ToInt32(_ctx));

        key.Free(_ctx);
        retrieved.Free(_ctx);
        keys.Free(_ctx);
        objectKeys.Free(_ctx);
        global.Free(_ctx);
        obj.Free(_ctx);
    }

    [Fact]
    public void TestAccessorProperty()
    {
        var obj = _ctx.NewObject();
        var key = _ctx.NewAtom("prop");

        var getter = _ctx.NewFunction((ctx, thisVal, args) =>
        {
            return ctx.NewInt32(100);
        }, "getter", 0);

        var setter = JSValue.Undefined;

        obj.DefineProperty(_ctx, key, getter, setter, JSPropertyFlags.Enumerable | JSPropertyFlags.Configurable);

        var val = obj.GetProperty(_ctx, key);
        Assert.Equal(100, val.ToInt32(_ctx));

        _ctx.GlobalObject.SetProperty(_ctx, "objWithGetter", obj);
        _ctx.Eval("objWithGetter.prop = 200");

        var valAfterSet = obj.GetProperty(_ctx, key);
        Assert.Equal(100, valAfterSet.ToInt32(_ctx)); // Still 100

        var result = _ctx.Eval("(() => { 'use strict'; try { objWithGetter.prop = 200; return 'success'; } catch(e) { return e.name; } })()");
        Assert.Equal("TypeError", result.ToString(_ctx));
        result.Free(_ctx);
        valAfterSet.Free(_ctx);
        val.Free(_ctx);
        key.Free(_ctx);
        obj.Free(_ctx);
    }

    [Fact]
    public void TestRawDefineProperty()
    {
        var obj = _ctx.NewObject();
        var key = _ctx.NewAtom("raw");
        var val = _ctx.NewInt32(7);

        var flags = JSPropertyFlags.Enumerable | JSPropertyFlags.HasValue | JSPropertyFlags.HasEnumerable;

        obj.DefinePropertyRaw(_ctx, key, val, JSValue.Undefined, JSValue.Undefined, flags);

        var retrieved = obj.GetProperty(_ctx, key);
        Assert.Equal(7, retrieved.ToInt32(_ctx));

        var keys = _ctx.Eval("Object.keys", "internal");
        var result = keys.Call(_ctx, [obj]);
        Assert.Equal(1, result.GetProperty(_ctx, "length").ToInt32(_ctx));

        key.Free(_ctx);
        val.Free(_ctx);
        retrieved.Free(_ctx);
        keys.Free(_ctx);
        result.Free(_ctx);
        obj.Free(_ctx);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _runtime.Dispose();
    }
}
