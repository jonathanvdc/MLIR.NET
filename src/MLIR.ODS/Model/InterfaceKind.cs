namespace MLIR.ODS.Model;

/// <summary>
/// Classifies the kind of an MLIR interface based on which interface base class it uses.
/// </summary>
public enum InterfaceKind
{
    /// <summary>
    /// An operation interface (<c>OpInterface</c> / <c>OpInterfaceTrait</c>).
    /// </summary>
    Op,

    /// <summary>
    /// A type interface (<c>TypeInterface</c>).
    /// </summary>
    Type,

    /// <summary>
    /// An attribute interface (<c>AttrInterface</c>).
    /// </summary>
    Attr,

    /// <summary>
    /// A dialect interface (<c>DialectInterface</c>).
    /// </summary>
    Dialect,
}
