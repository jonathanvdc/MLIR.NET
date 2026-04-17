using System.Net.Http.Headers;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

namespace MLIR.Dialects.Attributes;

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
    /// <param name="bitWidth">The bit width of the integer representation used to encode the enum values.</param>
    /// <param name="names">The mapping of enum values to their corresponding string representations.</param>
    public EnumAttributeAssemblyFormat(int bitWidth, IReadOnlyDictionary<ApInt, string> names)
    {
        Names = names;
        BitWidth = bitWidth;
        zero = ApInt.Zero(bitWidth);

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
    public int BitWidth { get; }

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
