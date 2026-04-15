namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Emits static constraint classes for ODS <c>Type</c> records.
/// </summary>
/// <remarks>
/// Each generated class is a static partial class that exposes a single
/// <c>TypeConstraintDefinition</c> property.  These classes do not derive from
/// <c>TypeReference</c> and do not expose a <c>TypeDefinition</c>; the runtime binder
/// handles all builtin type syntax (integer, float, tensor, …) natively.
///
/// Contrast with <see cref="TypeEmitter"/>, which handles ODS <c>TypeDef</c> records and
/// emits concrete TypeReference subclasses together with a <c>TypeDefinition</c>.
/// </remarks>
internal static class TypeConstraintEmitter
{
    /// <summary>
    /// Emits a <c>public static partial class</c> that exposes a single
    /// <c>TypeConstraintDefinition</c> property.
    /// </summary>
    public static void Emit(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        var nameArgument = typeConstraint.CanonicalTypeName != null
            ? EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName)
            : string.Empty;
        builder.AppendLine("public static partial class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeConstraintDefinition TypeConstraintDefinition { get; } =");
        builder.AppendLine("        new TypeConstraintDefinition(" + nameArgument + ");");
        builder.AppendLine("}");
    }

}
