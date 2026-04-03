namespace MLIR.Syntax.Types.Primitives;

/// <summary>
/// Identifies the signedness marker used by builtin integer types.
/// </summary>
public enum IntegerTypeSignedness
{
    /// <summary>
    /// The type is signless, such as <c>i32</c>.
    /// </summary>
    Signless,

    /// <summary>
    /// The type is explicitly signed, such as <c>si32</c>.
    /// </summary>
    Signed,

    /// <summary>
    /// The type is explicitly unsigned, such as <c>ui32</c>.
    /// </summary>
    Unsigned,
}
