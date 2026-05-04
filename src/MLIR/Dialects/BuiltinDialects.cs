namespace MLIR.Dialects;

/// <summary>
/// Convenience entry point for registering the full set of built-in dialects
/// shipped with the MLIR.NET runtime.
/// </summary>
/// <remarks>
/// This facade registers the prelude constraint library, the builtin dialect,
/// and the core operation dialects (arith, func). It is intentionally kept as
/// a thin layer over the individual generated registration classes so that
/// users do not need to discover each dialect's registration class manually.
/// </remarks>
public static class BuiltinDialects
{
    /// <summary>
    /// Creates a new <see cref="DialectRegistry"/> with all built-in dialects
    /// pre-registered.
    /// </summary>
    /// <returns>A new registry containing the prelude, builtin, arith, and func dialects.</returns>
    public static DialectRegistry CreateRegistry()
    {
        var registry = new DialectRegistry();
        RegisterAll(registry);
        return registry;
    }

    /// <summary>
    /// Registers all built-in dialects into the provided <paramref name="registry"/>.
    /// </summary>
    /// <param name="registry">The dialect registry to populate.</param>
    public static void RegisterAll(DialectRegistry registry)
    {
        registry.RegisterDialect(Prelude.PreludeDialectRegistration.Create());
        registry.RegisterDialect(Builtin.BuiltinDialectRegistration.Create());
        registry.RegisterDialect(Arith.ArithDialectRegistration.Create());
        registry.RegisterDialect(Func.FuncDialectRegistration.Create());
    }
}
