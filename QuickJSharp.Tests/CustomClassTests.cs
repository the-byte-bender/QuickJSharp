namespace QuickJSharp.Tests;

public class CustomClassTests : IDisposable
{
    private readonly JSRuntime _rt;
    private readonly JSContext _ctx;

    public CustomClassTests()
    {
        _rt = new JSRuntime();
        _ctx = _rt.CreateContext();
    }

    [Fact]
    public void Can_Create_New_ClassID()
    {
        var id1 = _rt.NewClassID();
        var id2 = _rt.NewClassID();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Can_Define_Class_And_Create_Instance()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "MyClass");

        var instance = _ctx.NewObjectClass(classId);
        Assert.True(instance.IsObject);
        // By default, no proto means no toString etc.
        Assert.True(
            _ctx.Eval("(obj) => Object.getPrototypeOf(obj) === null")
                .Call(_ctx, _ctx.Undefined, [instance])
                .ToBoolean(_ctx)
        );
    }

    [Fact]
    public void Can_Set_And_Get_Class_Proto()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "SimpleClass");

        var proto = _ctx.NewObject();
        proto.SetProperty(_ctx, "hello", _ctx.NewString("world"));

        _ctx.SetClassProto(classId, proto);

        var retrievedProto = _ctx.GetClassProto(classId);
        Assert.Equal("world", retrievedProto.GetProperty(_ctx, "hello").ToString(_ctx));

        var instance = _ctx.NewObjectClass(classId);
        Assert.Equal("world", instance.GetProperty(_ctx, "hello").ToString(_ctx));
    }

    [Fact]
    public void Can_Create_With_Explicit_Proto()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "ExplicitClass");

        var proto = _ctx.NewObject();
        proto.SetProperty(_ctx, "foo", _ctx.NewString("bar"));

        var instance = _ctx.NewObjectProtoClass(proto, classId);
        Assert.Equal("bar", instance.GetProperty(_ctx, "foo").ToString(_ctx));

        // Ensure it didn't affect the default proto
        var defaultInstance = _ctx.NewObjectClass(classId);
        Assert.True(defaultInstance.GetProperty(_ctx, "foo").IsUndefined);
    }

    [Fact]
    public void Can_Retrieve_ClassID_From_Instance()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "ClassIDTest");

        var instance = _ctx.NewObjectClass(classId);

        Assert.Equal(classId, instance.ClassID);
    }

    [Fact]
    public void Class_Call_Callback_Works()
    {
        var classId = _rt.NewClassID();
        bool called = false;
        _rt.DefineClass(
            classId,
            "CallableClass",
            call: (ctx, func, thisVal, args, flags) =>
            {
                called = true;
                return ctx.NewInt32(args[0].ToInt32(ctx) * 2);
            }
        );

        var instance = _ctx.NewObjectClass(classId);
        _ctx.GlobalObject.SetProperty(_ctx, "callable", instance);
        var result = _ctx.Eval("callable(21)");

        Assert.True(called);
        Assert.Equal(42, result.ToInt32(_ctx));
    }

    [Fact]
    public void Finalizer_Is_Called_On_Garbage_Collection()
    {
        var classId = _rt.NewClassID();
        bool finalized = false;

        _rt.DefineClass(
            classId,
            "FinalizableClass",
            finalizer: (rt, val) =>
            {
                finalized = true;
            }
        );

        {
            var instance = _ctx.NewObjectClass(classId);
            Assert.True(instance.IsObject);

            instance.Free(_ctx);
        }

        for (int i = 0; i < 10; i++)
        {
            _rt.RunGC();
        }

        Assert.True(finalized, "Finalizer was not called after reference was lost and RunGC was called");
    }

    [Fact]
    public void Finalizer_With_Opaque_Data_Works()
    {
        var classId = _rt.NewClassID();
        IntPtr finalizedOpaque = IntPtr.Zero;

        _rt.DefineClass(
            classId,
            "OpaqueFinalizableClass",
            (rt, val) =>
            {
                finalizedOpaque = val.Opaque;
            }
        );

        IntPtr myPtr = (IntPtr)unchecked((IntPtr)0xDEADBEEF);
        {
            var instance = _ctx.NewObjectClass(classId);
            instance.Opaque = myPtr;
            instance.Free(_ctx);
        }

        for (int i = 0; i < 10; i++)
            _rt.RunGC();

        Assert.Equal(myPtr, finalizedOpaque);
    }

    [Fact]
    public void SetConstructorBit_Syncs_Correctly()
    {
        var func = _ctx.NewFunction((ctx, thisVal, args) => ctx.Undefined, "TestFunc");
        _ctx.GlobalObject.SetProperty(_ctx, "TestFunc", func);

        // Initially it should NOT be a constructor
        Assert.False(func.IsConstructor(_ctx), "Expected IsConstructor to be false initially");
        var res1 = _ctx.Eval("new TestFunc()");
        Assert.True(res1.IsException, "Expected exception when calling new on a non-constructor");
        _ctx.ClearException();

        bool success = func.SetConstructorBit(_ctx, true);
        Assert.True(success);
        Assert.True(func.IsConstructor(_ctx), "Expected IsConstructor to be true after SetConstructorBit(true)");

        // Should now be constructible
        var result = _ctx.Eval("new TestFunc()");
        Assert.False(result.IsException, "Should not throw after setting constructor bit");
        Assert.True(result.IsUndefined);

        // Disable it again
        func.SetConstructorBit(_ctx, false);
        Assert.False(func.IsConstructor(_ctx), "Expected IsConstructor to be false after SetConstructorBit(false)");
        var res2 = _ctx.Eval("new TestFunc()");
        Assert.True(res2.IsException, "Expected exception after disabling constructor bit");
        _ctx.ClearException();
    }

    [Fact]
    public void Manual_Constructor_Pattern_Works()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "Point");

        var pointCtor = _ctx.NewFunction(
            (ctx, newTarget, args) =>
            {
                var proto = newTarget.GetProperty(ctx, "prototype");
                var instance = ctx.NewObjectProtoClass(proto, classId);

                instance.SetProperty(ctx, "x", args[0]);
                instance.SetProperty(ctx, "y", args[1]);

                return instance;
            },
            "Point",
            2
        );

        pointCtor.SetConstructorBit(_ctx, true);

        var pointProto = _ctx.NewObject();
        pointProto.SetProperty(_ctx, "getSum", _ctx.Eval("(function() { return this.x + this.y; })"));
        pointCtor.SetProperty(_ctx, "prototype", pointProto);
        _ctx.SetClassProto(classId, pointProto);

        _ctx.GlobalObject.SetProperty(_ctx, "Point", pointCtor);

        var result = _ctx.Eval("var p = new Point(10, 20); p.getSum()");
        Assert.Equal(30, result.ToInt32(_ctx));
        Assert.True(_ctx.Eval("p instanceof Point").ToBoolean(_ctx));
    }

    [Fact]
    public void Script_Class_Can_Extend_Native_Class_With_Opaque_Preserved()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "NativeBase");

        var nativeCtor = _ctx.NewFunction(
            (ctx, newTarget, args) =>
            {
                var proto = newTarget.GetProperty(ctx, "prototype");
                var instance = ctx.NewObjectProtoClass(proto, classId);

                instance.Opaque = (IntPtr)0xCC;
                return instance;
            },
            "NativeBase"
        );

        nativeCtor.SetConstructorBit(_ctx, true);
        var nativeProto = _ctx.NewObject();
        nativeCtor.SetProperty(_ctx, "prototype", nativeProto);
        _ctx.SetClassProto(classId, nativeProto);

        _ctx.GlobalObject.SetProperty(_ctx, "NativeBase", nativeCtor);

        var result = _ctx.Eval(
            @"
            class SubClass extends NativeBase {
                constructor(val) {
                    super();
                    this.subVal = val;
                }
            }
            new SubClass(123);
        "
        );

        Assert.True(result.IsObject);
        Assert.Equal(classId.Value, result.ClassID.Value);
        Assert.Equal((IntPtr)0xCC, result.Opaque);
        Assert.Equal(123, result.GetProperty(_ctx, "subVal").ToInt32(_ctx));

        Assert.True(
            _ctx.Eval("(obj) => obj instanceof NativeBase").Call(_ctx, _ctx.Undefined, [result]).ToBoolean(_ctx)
        );
    }

    [Fact]
    public void Use_Case_Class_Inheritance_Pattern()
    {
        var classId = _rt.NewClassID();
        _rt.DefineClass(classId, "CustomDate");

        // Inherit from standard Date
        var dateCtor = _ctx.GlobalObject.GetProperty(_ctx, "Date");
        var dateProto = dateCtor.GetProperty(_ctx, "prototype");

        var myProto = _ctx.NewObject();
        myProto.SetProperty(_ctx, "__proto__", dateProto); // Manual inheritance
        myProto.SetProperty(_ctx, "getCustomName", _ctx.Eval("(function() { return 'CustomDate'; })"));

        _ctx.SetClassProto(classId, myProto);

        var instance = _ctx.NewObjectClass(classId);

        _ctx.GlobalObject.SetProperty(_ctx, "instance", instance);
        var result = _ctx.Eval("instance.getCustomName()");
        Assert.Equal("CustomDate", result.ToString(_ctx));

        Assert.True(instance.GetProperty(_ctx, "getTime").IsFunction(_ctx));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _rt.Dispose();
    }
}
