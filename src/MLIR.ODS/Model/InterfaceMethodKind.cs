namespace MLIR.ODS.Model;

/// <summary>
/// Classifies the kind of an MLIR interface method.
/// </summary>
public enum InterfaceMethodKind
{
    /// <summary>
    /// A regular (non-static, non-pure-virtual) interface method with an optional body.
    /// </summary>
    Regular,

    /// <summary>
    /// A static interface method (<c>StaticInterfaceMethod</c>).
    /// </summary>
    Static,

    /// <summary>
    /// A pure virtual interface method that every implementer must define (<c>PureVirtualInterfaceMethod</c>).
    /// </summary>
    PureVirtual,

    /// <summary>
    /// An interface method declaration without an inline body (<c>InterfaceMethodDeclaration</c>).
    /// </summary>
    Declaration,
}
