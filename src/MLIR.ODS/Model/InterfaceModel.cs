namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an MLIR interface definition imported from a TableGen <c>Interface</c> record
/// (including <c>OpInterface</c>, <c>TypeInterface</c>, <c>AttrInterface</c>, and
/// <c>DialectInterface</c>).
/// </summary>
/// <remarks>
/// Interface records in MLIR TableGen carry the C++ interface name, namespace, optional
/// description, a list of method declarations, and zero or more base interfaces.
/// This model is the foundational representation used by later layers that generate C#
/// interface types or verify interface constraints.
/// </remarks>
public sealed class InterfaceModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceModel"/>.
    /// </summary>
    public InterfaceModel(
        string recordName,
        InterfaceKind kind,
        string cppInterfaceName,
        string? cppNamespace = null,
        string? description = null,
        IReadOnlyList<string>? baseInterfaces = null,
        IReadOnlyList<InterfaceMethodModel>? methods = null,
        string? csharpName = null,
        IReadOnlyList<InterfaceCSharpMemberModel>? csharpMembers = null)
    {
        RecordName = recordName;
        Kind = kind;
        CppInterfaceName = cppInterfaceName;
        CppNamespace = cppNamespace;
        Description = description;
        BaseInterfaces = baseInterfaces ?? EmptyStrings;
        Methods = methods ?? EmptyMethods;
        CsharpName = csharpName;
        CsharpMembers = csharpMembers ?? EmptyCsharpMembers;
    }

    /// <summary>
    /// Gets the originating TableGen record name (e.g., <c>"ShapedTypeInterface"</c>,
    /// <c>"Symbol"</c>).
    /// </summary>
    public string RecordName { get; }

    /// <summary>
    /// Gets the interface kind: op, type, attribute, or dialect.
    /// </summary>
    public InterfaceKind Kind { get; }

    /// <summary>
    /// Gets the C++ interface class name as specified in the <c>cppInterfaceName</c> field.
    /// </summary>
    public string CppInterfaceName { get; }

    /// <summary>
    /// Gets the C++ namespace for this interface as specified in the <c>cppNamespace</c>
    /// field. May be <see langword="null"/> or empty if the namespace was not set.
    /// </summary>
    public string? CppNamespace { get; }

    /// <summary>
    /// Gets the human-readable description of the interface, if present.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the record names of the base interfaces declared in the <c>baseInterfaces</c>
    /// list of this interface record. Order matches the declaration order.
    /// </summary>
    public IReadOnlyList<string> BaseInterfaces { get; }

    /// <summary>
    /// Gets the method declarations for this interface.
    /// </summary>
    public IReadOnlyList<InterfaceMethodModel> Methods { get; }

    /// <summary>
    /// Gets the optional C# interface name override supplied by MLIR.NET overlay metadata.
    /// </summary>
    public string? CsharpName { get; }

    /// <summary>
    /// Gets the explicit C# members that should be emitted for this interface.
    /// </summary>
    public IReadOnlyList<InterfaceCSharpMemberModel> CsharpMembers { get; }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<InterfaceMethodModel> EmptyMethods = new InterfaceMethodModel[0];
    private static readonly IReadOnlyList<InterfaceCSharpMemberModel> EmptyCsharpMembers = new InterfaceCSharpMemberModel[0];
}
