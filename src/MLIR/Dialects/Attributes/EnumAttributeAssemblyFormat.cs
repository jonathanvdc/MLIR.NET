using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

namespace MLIR.Dialects.Attributes;

/// <summary>
/// Defines the requirement for angle brackets in the assembly syntax of enum attribute values.
/// This enum is used to specify whether angle brackets are required, optional, or prohibited in the assembly syntax for enum attribute values when using the <see cref="EnumAttributeAssemblyFormat{T}"/> class.
/// The requirement for angle brackets can affect how the assembly syntax for enum attribute values is parsed and printed, allowing for flexibility in the assembly format based on the specific needs of the enum attribute type
/// </summary>
public enum EnumAngleBracketRequirement
{
    /// <summary>
    /// Indicates that angle brackets are required in the assembly syntax for enum attribute values. If this requirement is specified, then the assembly syntax for enum attribute values must include angle brackets around the enum elements, such as <c>&lt;EnumValue&gt;</c>.
    /// If angle brackets are not present in the assembly syntax for enum attribute values when this requirement is specified, it will be considered a syntax error during parsing.
    /// </summary>
    Required,

    /// <summary>
    /// Indicates that angle brackets are optional in the assembly syntax for enum attribute values. If this requirement is specified, then the assembly syntax for enum attribute values may include angle brackets around the enum elements
    /// such as <c>&lt;EnumValue&gt;</c>, but it is not mandatory. The assembly syntax for enum attribute values can be valid with or without angle brackets when this requirement is specified.
    /// During parsing, both forms of assembly syntax for enum attribute values (with or without angle brackets) will be accepted as valid when this requirement is specified.
    /// </summary>
    Optional,

    /// <summary>
    /// Indicates that angle brackets are prohibited in the assembly syntax for enum attribute values. If this requirement is specified, then the assembly syntax for enum attribute values must not include angle brackets around the enum
    /// elements, and the enum elements must be directly present without delimiters, such as <c>EnumValue</c>. If angle brackets are present in the assembly syntax for enum attribute values when this requirement is specified, it will be considered a syntax error during parsing.
    /// </summary>
    Prohibited
}

/// <summary>
/// Provides an assembly format for attributes whose values are enums represented as bitfields, allowing them to be printed and parsed
/// in assembly form using their string representations defined in the <see cref="Names"/> mapping.
/// The <see cref="EnumAttributeAssemblyFormat{T}"/> class is a generic base class that can be used to define assembly formats for specific
/// enum attribute types by providing the necessary mappings and conversion logic between the enum values and their integer representations.
/// </summary>
/// <typeparam name="T">
/// The specific enum attribute type for which this assembly format is defined.
/// This type must derive from <see cref="AttributeValue"/> and represents the strongly-typed enum values that correspond to the integer bitfield
/// representations used in the assembly form of the attribute values.
/// </typeparam>
public abstract class EnumAttributeAssemblyFormat<T> : IAttributeAssemblyFormat where T : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumAttributeAssemblyFormat{T}"/> class with the specified bit width and mapping of enum values to their string representations.
    /// </summary>
    /// <param name="names">The mapping of enum values to their corresponding string representations.</param>
    public EnumAttributeAssemblyFormat(IReadOnlyDictionary<ApInt, string> names)
    {
        Names = names;
        zero = ApInt.Zero(BitWidth);

        reverseNames = new Dictionary<string, ApInt>(StringComparer.Ordinal);
        foreach (var pair in names)
        {
            reverseNames[pair.Value] = pair.Key;
        }
    }

    /// <summary>
    /// Gets the mapping of enum values of type <typeparamref name="T"/> to their corresponding string representations.
    /// This mapping is used to convert between the strongly-typed enum values and their string representations when parsing and printing enum attribute values in assembly form.
    /// For example, if <typeparamref name="T"/> is an enum type representing different optimization levels, the <see cref="Names"/> dictionary might map enum values like
    /// <c>OptimizationLevel.O0</c>, <c>OptimizationLevel.O1</c>, <c>OptimizationLevel.O2</c>, and <c>OptimizationLevel.O3</c> to their corresponding string representations
    /// like "O0", "O1", "O2", and "O3".
    /// </summary>
    public IReadOnlyDictionary<ApInt, string> Names { get; }

    /// <summary>
    /// Gets the bit width of the integer representation used to encode the enum values of type <typeparamref name="T"/> in the enum attribute value.
    /// This bit width determines how many bits are used to represent the enum values as integers when they are stored or transmitted as part of the enum attribute value.
    /// For example, if the enum values can be represented within 8 bits, the <see cref="BitWidth"/> might be set to 8.
    /// </summary>
    public abstract int BitWidth { get; }

    /// <summary>
    /// Gets the zero value of the integer representation for the enum values of type <typeparamref name="T"/>.
    /// This is used as the initial value for the accumulator when parsing enum attribute values from their string representations in assembly form.
    /// </summary>
    protected readonly ApInt zero;

    /// <summary>
    /// Gets the reverse mapping of enum value string representations to their corresponding integer values.
    /// This is used for parsing enum attribute values from their string representations in assembly form.
    /// </summary>
    protected readonly Dictionary<string, ApInt> reverseNames;

    /// <summary>
    /// Gets the requirement for angle brackets in the assembly syntax of enum attribute values for this assembly format. This property specifies whether angle brackets are required, optional, or prohibited in the assembly syntax for enum attribute values when using this assembly format.
    /// The requirement for angle brackets can affect how the assembly syntax for enum attribute values is parsed
    /// </summary>
    public abstract EnumAngleBracketRequirement AngleBracketRequirement { get; }

    /// <summary>
    /// Converts an integer value to the corresponding enum value of type <typeparamref name="T"/>.
    /// This method is used to interpret the integer representation of the enum attribute value and convert it back to the strongly-typed enum value that it represents.
    /// </summary>
    /// <param name="value">The integer value to convert to the corresponding enum value of type <typeparamref name="T"/>.</param>
    /// <param name="syntax">The original syntax node from which the integer value was parsed, used for error reporting if the integer value does not correspond to a valid enum value.</param>
    /// <returns>The enum value of type <typeparamref name="T"/> that corresponds to the given integer value.</returns>
    public abstract T EnumFromInt(ApInt value, AttributeValueSyntax syntax);

    /// <summary>
    /// Converts an enum value of type <typeparamref name="T"/> to its corresponding integer representation.
    /// This method is used to convert a strongly-typed enum value to its integer representation, which can then be used for storage or transmission as part of the enum attribute value.
    /// </summary>
    /// <param name="value">The enum value of type <typeparamref name="T"/> to convert to its corresponding integer representation.</param>
    /// <returns>The integer representation of the given enum value of type <typeparamref name="T"/>.</returns>
    public abstract ApInt EnumToInt(T value);

    /// <inheritdoc/>
    public abstract AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder);

    /// <inheritdoc/>
    public abstract AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context);

    /// <inheritdoc/>
    public abstract ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context);
}
