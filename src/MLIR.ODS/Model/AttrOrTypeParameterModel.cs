namespace MLIR.ODS.Model;

/// <summary>
/// Represents a single parameter in the <c>parameters</c> DAG of an <c>AttrDef</c> or <c>TypeDef</c> record,
/// corresponding to a subclass of <c>AttrOrTypeParameter</c> (or its string shorthand) from
/// upstream MLIR's <c>AttrTypeBase.td</c>.
/// </summary>
/// <remarks>
/// In TableGen ODS, the <c>parameters</c> dag of an attribute or type definition describes the
/// set of parameters that must be supplied when constructing a value of that attribute or type.
/// Each parameter entry is either a plain C++ type string (shorthand form, e.g., <c>"unsigned":$width</c>)
/// or an instantiation of a named <c>AttrOrTypeParameter</c> subclass such as
/// <c>StringRefParameter&lt;"desc"&gt;:$name</c>.
/// </remarks>
public sealed class AttrOrTypeParameterModel(
    string name,
    string? constraintRecordName,
    string cppType,
    string? cppStorageType = null,
    string? cppAccessorType = null,
    string? summary = null,
    string? defaultValue = null,
    string? csharpType = null)
{
    /// <summary>
    /// Gets the parameter name as declared in the <c>parameters</c> dag (e.g., <c>"value"</c> from <c>$value</c>).
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the name of the originating <c>AttrOrTypeParameter</c> subclass record, if the parameter
    /// was specified via a named class rather than an inline string.
    /// Examples: <c>"StringRefParameter"</c>, <c>"APIntParameter"</c>, <c>"ArrayRefParameter"</c>.
    /// Null when the parameter was specified as a plain C++ type string.
    /// </summary>
    public string? ConstraintRecordName { get; } = constraintRecordName;

    /// <summary>
    /// Gets the C++ type of this parameter (e.g., <c>"::llvm::StringRef"</c>, <c>"unsigned"</c>).
    /// </summary>
    public string CppType { get; } = cppType;

    /// <summary>
    /// Gets the C++ storage type of this parameter, if it differs from <see cref="CppType"/>.
    /// For example, <c>StringRefParameter</c> stores as <c>"std::string"</c> but exposes <c>"::llvm::StringRef"</c>.
    /// Null when the storage type is the same as <see cref="CppType"/>.
    /// </summary>
    public string? CppStorageType { get; } = cppStorageType;

    /// <summary>
    /// Gets the C++ accessor type of this parameter, if it differs from <see cref="CppType"/>.
    /// For example, <c>APIntParameter</c> has accessor type <c>"const ::llvm::APInt &amp;"</c>.
    /// Null when the accessor type is the same as <see cref="CppType"/>.
    /// </summary>
    public string? CppAccessorType { get; } = cppAccessorType;

    /// <summary>
    /// Gets the human-readable summary description of this parameter, if provided.
    /// </summary>
    public string? Summary { get; } = summary;

    /// <summary>
    /// Gets the C++ default value expression for this parameter (e.g., <c>"std::string()"</c>),
    /// or null if the parameter has no default value and is therefore required.
    /// </summary>
    public string? DefaultValue { get; } = defaultValue;

    /// <summary>
    /// Gets a value indicating whether this parameter has a default value and may therefore be omitted.
    /// </summary>
    public bool HasDefaultValue => DefaultValue != null;

    /// <summary>
    /// Gets the mapped C# type for this parameter, if known from an
    /// <c>MLIRNet_AttrOrTypeParameterExtension</c> declaration or a built-in class mapping.
    /// For example, <c>StringRefParameter</c> maps to <c>"string"</c>.
    /// Null when no C# type mapping is available.
    /// </summary>
    public string? CsharpType { get; } = csharpType;
}
