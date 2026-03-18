namespace QuickJSharp.Tests;

public class SandboxingTests : IDisposable
{
    private readonly JSRuntime _rt;

    public SandboxingTests()
    {
        _rt = new JSRuntime();
    }

    [Fact]
    public void Sandbox_EscapeAttempt_ProtoPollution()
    {
        using var ctx1 = _rt.CreateContext();
        using var ctx2 = _rt.CreateContext();

        // Pollute Object.prototype in ctx1
        ctx1.Eval("Object.prototype.pwned = true");
        Assert.True(ctx1.Eval("({}).pwned").ToBoolean(ctx1));

        // Ensure ctx2 is not polluted initially
        Assert.True(ctx2.Eval("({}).pwned === undefined").ToBoolean(ctx2));

        var val1 = ctx1.Eval("({ a: 1 })");
        var bytecode = ctx1.WriteObject(val1);
        Assert.NotNull(bytecode);

        var val2 = ctx2.ReadObject(bytecode);

        // In ctx2, the transferred value should NOT have the polluted prototype from ctx1
        var pwnedIn2 = val2.GetProperty(ctx2, "pwned");
        Assert.True(pwnedIn2.IsUndefined);

        // And ctx2's global prototype remains clean
        Assert.True(ctx2.Eval("({}).pwned === undefined").ToBoolean(ctx2));
    }

    [Fact]
    public void Sandbox_MultipleSerialization_Consistency()
    {
        using var ctx = _rt.CreateContext();
        var val = ctx.Eval("({ x: 1 })");

        var b1 = ctx.WriteObject(val);
        var b2 = ctx.WriteObject(val);
        Assert.Equal(b1, b2);

        var val1 = ctx.ReadObject(b1);
        var val2 = ctx.ReadObject(b1);

        Assert.Equal(1, val1.GetProperty(ctx, "x").ToInt32(ctx));
        Assert.Equal(1, val2.GetProperty(ctx, "x").ToInt32(ctx));
    }

    [Fact]
    public void Sandbox_CircularReference_WithFlags()
    {
        using var ctx1 = _rt.CreateContext();
        using var ctx2 = _rt.CreateContext();

        ctx1.Eval("var obj = { a: 1 }; obj.self = obj;");
        var obj1 = ctx1.Eval("obj");

        var bytecode = ctx1.WriteObject(obj1, JSWriteObjectFlags.Reference);
        Assert.NotNull(bytecode);

        var obj2 = ctx2.ReadObject(bytecode, JSReadObjectFlags.Reference);
        Assert.Equal(1, obj2.GetProperty(ctx2, "a").ToInt32(ctx2));

        var checker = ctx2.Eval("(obj => obj.self === obj)");
        Assert.True(checker.Call(ctx2, [obj2]).ToBoolean(ctx2));
    }

    // This is expected behavior.
    // Contexts share a runtime and can freely operate on each other's values, the caller is responsible for not passing values directly between contexts if they want to sandbox them. Use WriteObject/ReadObject for safe cross-context transfer.
    [Fact]
    public void Sandbox_DirectTransfer_AllowsPrototypePollution()
    {
        using var ctx1 = _rt.CreateContext();
        using var ctx2 = _rt.CreateContext();

        // Victim context (ctx1) has a clean prototype
        Assert.True(ctx1.Eval("({}).pwned === undefined").ToBoolean(ctx1));

        // Create an object in ctx1
        var victimObj = ctx1.Eval("({ name: 'victim' })");

        // Attacker function in ctx2 that pollutes the prototype of whatever it's passed
        var attackerFunc = ctx2.Eval(
            @"
            (function(obj) {
                Object.getPrototypeOf(obj).pwned = 'pwned by ctx2';
            })
        "
        );

        attackerFunc.Call(ctx2, [victimObj]);

        var pwnedIn1 = ctx1.Eval("({}).pwned").ToString(ctx1);
        Assert.Equal("pwned by ctx2", pwnedIn1);
    }

    public void Dispose()
    {
        _rt.Dispose();
    }
}
