namespace MLIR.ODS.Model;

/// <summary>
/// Describes the public shape used for optional generated operation attributes.
/// </summary>
public enum OptionalAttributeRepresentation
{
    /// <summary>Expose the optional attribute as a nullable public value.</summary>
    NullableValue,

    /// <summary>Expose the optional attribute as a Boolean that indicates presence.</summary>
    PresenceBoolean,
}
