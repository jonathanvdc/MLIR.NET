namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents an immutable semantic type value bound from concrete syntax.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TypeReference"/> carries optional source provenance through <see cref="Syntax"/> and
/// <see cref="Location"/>, but equality is defined only in terms of semantic type identity.
/// Two type values compare equal when they describe the same type even if they were parsed from
/// different syntax nodes or one side was synthesized later.
/// </para>
/// <para>
/// Concrete type families override <see cref="SemanticFamily"/>, <see cref="SemanticEqualsValue"/>,
/// and <see cref="GetSemanticHashCodeValue"/> to describe the parts of the type that matter for
/// semantic identity.
/// </para>
/// </remarks>
public abstract class TypeReference : IEquatable<TypeReference>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReference"/> class.
    /// </summary>
    protected TypeReference(TypeSyntax? syntax)
    {
        Syntax = syntax;
    }

    /// <summary>
    /// Gets the syntax for the type, or <see langword="null"/> if this is a synthetic type with no preserved source text.
    /// </summary>
    public TypeSyntax? Syntax { get; }

    /// <summary>
    /// Gets the canonical type name, if one was recognized.
    /// </summary>
    public abstract string? Name { get; }

    /// <summary>
    /// Gets the registered definition, if one exists.
    /// </summary>
    public abstract TypeDefinition? Definition { get; }

    /// <summary>
    /// Gets a value indicating whether the type was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the source location of the type text, if known.
    /// </summary>
    public SourceLocation Location => Syntax?.Location ?? SourceLocation.Unknown;

    /// <summary>
    /// Compares this type value to another one using semantic type identity.
    /// Source syntax and source locations are ignored.
    /// </summary>
    public bool Equals(TypeReference? other)
    {
        return other != null
            && (ReferenceEquals(this, other)
                || (SemanticFamily == other.SemanticFamily && SemanticEqualsValue(other)));
    }

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj)
    {
        return obj is TypeReference other && Equals(other);
    }

    /// <inheritdoc/>
    public sealed override int GetHashCode()
    {
        unchecked
        {
            return (SemanticFamily.GetHashCode() * 397) ^ GetSemanticHashCodeValue();
        }
    }

    /// <summary>
    /// Compares two type values using semantic type identity.
    /// </summary>
    public static bool operator ==(TypeReference? left, TypeReference? right)
    {
        return EqualityComparer<TypeReference?>.Default.Equals(left, right);
    }

    /// <summary>
    /// Compares two type values for semantic inequality.
    /// </summary>
    public static bool operator !=(TypeReference? left, TypeReference? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Gets the semantic type family used to partition equality comparisons.
    /// Families allow generated wrappers to compare equal to the builtin families they represent
    /// while keeping unrelated types with the same name distinct.
    /// </summary>
    protected virtual Type SemanticFamily => GetType();

    /// <summary>
    /// Compares the semantic payload of two type values from the same <see cref="SemanticFamily"/>.
    /// </summary>
    protected virtual bool SemanticEqualsValue(TypeReference other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Computes the hash code for the semantic payload of this type value.
    /// </summary>
    protected virtual int GetSemanticHashCodeValue()
    {
        return Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0;
    }

    /// <summary>
    /// Computes a stable hash code for a sequence of nested type values.
    /// </summary>
    protected static int GetSequenceHashCode(IReadOnlyList<TypeReference> values)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < values.Count; i++)
            {
                hash = (hash * 31) + values[i].GetHashCode();
            }

            return hash;
        }
    }

    /// <summary>
    /// Computes a stable hash code for a sequence of nullable dimensions.
    /// </summary>
    protected static int GetSequenceHashCode(IReadOnlyList<long?> values)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < values.Count; i++)
            {
                hash = (hash * 31) + (values[i]?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }
}
