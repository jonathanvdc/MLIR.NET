namespace MLIR.ODS.Model;

/// <summary>
/// Describes the kind of member represented in an ODS operation definition.
/// </summary>
public enum OperationMemberKind
{
    /// <summary>An operand of the operation.</summary>
    Operand,
    /// <summary>A result produced by the operation.</summary>
    Result,
    /// <summary>An attribute attached to the operation.</summary>
    Attribute,
    /// <summary>A region attached to the operation.</summary>
    Region,
}

/// <summary>
/// Represents a logical operation member imported from ODS.
/// </summary>
public abstract class OperationMemberModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationMemberModel"/>.
    /// </summary>
    protected OperationMemberModel(
        string name,
        string? constraintRecordName = null,
        bool isOptional = false,
        OperationMemberKind kind = OperationMemberKind.Operand)
    {
        Name = name;
        ConstraintRecordName = constraintRecordName;
        IsOptional = isOptional;
        Kind = kind;
    }

    /// <summary>
    /// Gets the logical member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the originating ODS constraint record name, if one was present.
    /// </summary>
    public string? ConstraintRecordName { get; }

    /// <summary>
    /// Gets a value indicating whether the member is optional.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// Gets the member kind.
    /// </summary>
    public OperationMemberKind Kind { get; }
}

/// <summary>
/// Represents an operand imported from ODS.
/// </summary>
public sealed class OperandModel : OperationMemberModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperandModel"/>.
    /// </summary>
    public OperandModel(string name, string? constraintRecordName = null, bool isOptional = false, bool isVariadic = false)
        : base(name, constraintRecordName, isOptional, OperationMemberKind.Operand)
    {
        IsVariadic = isVariadic;
    }

    /// <summary>
    /// Gets a value indicating whether this operand accepts zero or more values (variadic).
    /// </summary>
    public bool IsVariadic { get; }
}

/// <summary>
/// Represents a result imported from ODS.
/// </summary>
public sealed class ResultModel : OperationMemberModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="ResultModel"/>.
    /// </summary>
    public ResultModel(string name, string? constraintRecordName = null, bool isOptional = false, bool isVariadic = false)
        : base(name, constraintRecordName, isOptional, OperationMemberKind.Result)
    {
        IsVariadic = isVariadic;
    }

    /// <summary>
    /// Gets a value indicating whether this result may produce zero or more values (variadic).
    /// </summary>
    public bool IsVariadic { get; }
}

/// <summary>
/// Represents an attribute use imported from ODS.
/// </summary>
public sealed class AttributeUseModel : OperationMemberModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="AttributeUseModel"/>.
    /// </summary>
    public AttributeUseModel(string name, string? constraintRecordName = null, bool isOptional = false)
        : base(name, constraintRecordName, isOptional, OperationMemberKind.Attribute)
    {
    }
}

/// <summary>
/// Represents a region imported from ODS.
/// </summary>
public sealed class RegionModel : OperationMemberModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegionModel"/> class.
    /// </summary>
    public RegionModel(string name, string? constraintRecordName = null, bool isOptional = false, bool isVariadic = false)
        : base(name, constraintRecordName, isOptional, OperationMemberKind.Region)
    {
        IsVariadic = isVariadic;
    }

    /// <summary>
    /// Gets a value indicating whether this region accepts zero or more regions.
    /// </summary>
    public bool IsVariadic { get; }
}
