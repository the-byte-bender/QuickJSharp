namespace QuickJSharp.Bindings.Generators;

static class BindingConstants
{
    public const string JSExposeAttribute = "QuickJSharp.Bindings.JSExposeAttribute";
    public const string JSNameAttribute = "QuickJSharp.Bindings.JSNameAttribute";
    public const string JSBindingsRegistryAttribute = "QuickJSharp.Bindings.JSBindingsRegistryAttribute";
    public const string JSNamingConventionAttribute = "QuickJSharp.Bindings.JSNamingConventionAttribute";
    public const string BindingExtensionBaseType = "QuickJSharp.Bindings.BindingsRegistry";
    public const string AggressiveInlining =
        "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
    public const string ModuleInitializer = "[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]";

    public static class JSExposeParams
    {
        public const string ExposeImplicitConstructor = "ExposeImplicitConstructor";
        public const string Read = "Read";
        public const string Write = "Write";
    }

    public static class JSNamingConventionParams
    {
        public const string Constructors = "Constructors";
        public const string Globals = "Globals";
        public const string EnumMembers = "EnumMembers";
        public const string Members = "Members";
        public const string Constants = "Constants";
    }
}
