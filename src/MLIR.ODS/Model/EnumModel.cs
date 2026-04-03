namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an enum definition imported from ODS (from <c>EnumInfo</c> or its subclasses).
/// </summary>
public sealed class EnumModel(
    string className,
    string? cppNamespace,
    int bitwidth,
    bool isBitEnum,
    string separator,
    IReadOnlyList<EnumCaseModel> cases)
{
    /// <summary>
    /// Gets the C# (and C++) enum class name.
    /// </summary>
    public string ClassName { get; } = className;

    /// <summary>
    /// Gets the C++ namespace for the enum, if specified.
    /// </summary>
    public string? CppNamespace { get; } = cppNamespace;

    /// <summary>
    /// Gets the underlying integer bitwidth (8, 16, 32, or 64).
    /// </summary>
    public int Bitwidth { get; } = bitwidth;

    /// <summary>
    /// Gets a value indicating whether this is a bit-flag enum (uses bitwise OR combination).
    /// </summary>
    public bool IsBitEnum { get; } = isBitEnum;

    /// <summary>
    /// Gets the separator string used when printing multiple bit flags (<c>"|"</c> or <c>","</c>).
    /// Only meaningful when <see cref="IsBitEnum"/> is <see langword="true"/>.
    /// </summary>
    public string Separator { get; } = separator;

    /// <summary>
    /// Gets the list of all declared enum cases.
    /// </summary>
    public IReadOnlyList<EnumCaseModel> Cases { get; } = cases;
}
