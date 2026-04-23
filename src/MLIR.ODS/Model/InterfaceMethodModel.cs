namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents a single method declared by an MLIR interface, as imported from an
/// <c>InterfaceMethod</c> (or one of its specializations) record in TableGen.
/// </summary>
/// <remarks>
/// Upstream MLIR defines several method flavors:
/// <list type="bullet">
/// <item><term><c>InterfaceMethod</c></term><description>Regular method with an optional body.</description></item>
/// <item><term><c>StaticInterfaceMethod</c></term><description>Static method variant.</description></item>
/// <item><term><c>PureVirtualInterfaceMethod</c></term><description>Pure virtual; no default body.</description></item>
/// <item><term><c>InterfaceMethodDeclaration</c></term><description>Forward declaration only.</description></item>
/// </list>
/// C++ signatures are stored verbatim as strings because translating arbitrary C++ to C# is
/// out of scope for this foundational layer.
/// </remarks>
public sealed class InterfaceMethodModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceMethodModel"/>.
    /// </summary>
    public InterfaceMethodModel(
        string name,
        string returnType,
        InterfaceMethodKind kind,
        string? description = null,
        IReadOnlyList<(string ArgType, string ArgName)>? arguments = null,
        string? body = null,
        string? defaultBody = null)
    {
        Name = name;
        ReturnType = returnType;
        Kind = kind;
        Description = description;
        Arguments = arguments ?? EmptyArguments;
        Body = body;
        DefaultBody = defaultBody;
    }

    /// <summary>
    /// Gets the method name as specified in the <c>name</c> field of the <c>InterfaceMethod</c> record.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the C++ return type string as specified in the <c>returnType</c> field.
    /// </summary>
    public string ReturnType { get; }

    /// <summary>
    /// Gets the method flavor (regular, static, pure-virtual, or declaration).
    /// </summary>
    public InterfaceMethodKind Kind { get; }

    /// <summary>
    /// Gets the human-readable description of the method, if one was provided in the
    /// <c>description</c> field.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the list of C++ argument type/name pairs extracted from the <c>arguments</c> dag.
    /// Types and names are preserved verbatim as C++ strings.
    /// </summary>
    public IReadOnlyList<(string ArgType, string ArgName)> Arguments { get; }

    /// <summary>
    /// Gets the optional inline body code block from the <c>body</c> field. Empty or absent
    /// means there is no inline body.
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// Gets the optional default implementation body from the <c>defaultBody</c> field. Empty
    /// or absent means there is no default implementation.
    /// </summary>
    public string? DefaultBody { get; }

    private static readonly IReadOnlyList<(string, string)> EmptyArguments = new (string, string)[0];
}
