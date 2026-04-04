namespace MLIR.Semantics.Attributes.Collections;

using MLIR.Semantics;

/// <summary>
/// Represents a semantic dictionary attribute value.
/// </summary>
public abstract class DictionaryAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryAttributeValue"/> class.
    /// </summary>
    protected DictionaryAttributeValue(AttributeValueConstructionContext context, NamedAttributeCollection attributes)
        : base(context.Syntax, context.Location)
    {
        Attributes = attributes;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DictionaryAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DictionaryAttributeValue(NamedAttributeCollection attributes)
        : base(null, SourceLocation.Unknown)
    {
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the decoded entries.
    /// </summary>
    public NamedAttributeCollection Attributes { get; }
}
