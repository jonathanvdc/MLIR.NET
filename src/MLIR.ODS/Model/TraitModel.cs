namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents a trait on an ODS entity (operation, attribute, or type), as modeled from TableGen.
/// Concrete subclasses correspond to the trait class hierarchy in MLIR's <c>Traits.td</c>:
/// <c>NativeTrait</c>, <c>TraitList</c>, <c>GenInternalTrait</c>, and plain <c>Trait</c>
/// subclasses that do not fall into those categories.
/// </summary>
public abstract class TraitModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="TraitModel"/>.
    /// </summary>
    protected TraitModel(string recordName)
    {
        RecordName = recordName;
    }

    /// <summary>
    /// Gets the originating TableGen record name (e.g., <c>"Pure"</c>, <c>"Commutative"</c>,
    /// <c>"IsolatedFromAbove"</c>). This is the symbolic name used to reference the trait in
    /// operation definitions.
    /// </summary>
    public string RecordName { get; }
}

/// <summary>
/// Represents a <c>NativeTrait</c> (or its subclass <c>NativeOpTrait</c>) from MLIR's
/// <c>Traits.td</c>. Native traits map directly to a C++ trait type, identified by
/// <see cref="Trait"/> and <see cref="CppNamespace"/>.
/// </summary>
public sealed class NativeTraitModel : TraitModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="NativeTraitModel"/>.
    /// </summary>
    public NativeTraitModel(string recordName, string? trait, string? cppNamespace)
        : base(recordName)
    {
        Trait = trait;
        CppNamespace = cppNamespace;
    }

    /// <summary>
    /// Gets the C++ trait identifier as specified in the <c>trait</c> field of the TableGen
    /// record (e.g., <c>"IsCommutative"</c>, <c>"IsIsolatedFromAbove"</c>). May be
    /// <see langword="null"/> if the field was absent.
    /// </summary>
    public string? Trait { get; }

    /// <summary>
    /// Gets the C++ namespace for the trait class as specified in the <c>cppNamespace</c>
    /// field (e.g., <c>"::mlir::OpTrait"</c>). May be <see langword="null"/> if the field
    /// was absent.
    /// </summary>
    public string? CppNamespace { get; }
}

/// <summary>
/// Represents a <c>TraitList</c> from MLIR's <c>Traits.td</c>. A trait list groups multiple
/// traits together and is itself a <c>Trait</c>, allowing convenient shorthand names like
/// <c>Pure</c> to expand to a set of constituent traits.
/// </summary>
public sealed class TraitListModel : TraitModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="TraitListModel"/>.
    /// </summary>
    public TraitListModel(string recordName, IReadOnlyList<TraitModel> traits)
        : base(recordName)
    {
        Traits = traits;
    }

    /// <summary>
    /// Gets the constituent traits grouped under this list.
    /// </summary>
    public IReadOnlyList<TraitModel> Traits { get; }
}

/// <summary>
/// Represents a <c>GenInternalTrait</c> from MLIR's <c>Traits.td</c>. Generator-internal
/// traits affect code-generation behavior rather than mapping directly to a C++ trait type.
/// </summary>
public sealed class GenInternalTraitModel : TraitModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="GenInternalTraitModel"/>.
    /// </summary>
    public GenInternalTraitModel(string recordName, string? trait)
        : base(recordName)
    {
        Trait = trait;
    }

    /// <summary>
    /// Gets the generator-internal trait identifier as specified in the <c>trait</c> field
    /// (e.g., <c>"::mlir::OpTrait::AttrSizedOperandSegments"</c>). May be
    /// <see langword="null"/> if the field was absent.
    /// </summary>
    public string? Trait { get; }
}

/// <summary>
/// Represents a <c>Trait</c> record that does not fall into any of the more specific
/// recognized categories (<c>NativeTrait</c>, <c>TraitList</c>, <c>GenInternalTrait</c>).
/// This covers custom or interface-backed traits defined in terms of the base <c>Trait</c>
/// class.
/// </summary>
public sealed class SimpleTraitModel : TraitModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="SimpleTraitModel"/>.
    /// </summary>
    public SimpleTraitModel(string recordName)
        : base(recordName)
    {
    }
}
