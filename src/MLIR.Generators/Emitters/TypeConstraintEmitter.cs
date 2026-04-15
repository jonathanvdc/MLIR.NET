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
    public static void Emit(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitStaticConstraintClass(builder, typeConstraint);
    }

    /// <summary>
    /// Emits a <c>public static partial class</c> that exposes a single
    /// <c>TypeConstraintDefinition</c> property.
    /// </summary>
    private static void EmitStaticConstraintClass(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        builder.AppendLine("public static partial class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeConstraintDefinition TypeConstraintDefinition { get; } =");
        if (typeConstraint.CanonicalTypeName != null)
        {
            builder.AppendLine("        new TypeConstraintDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName) + ");");
        }
        else
        {
            builder.AppendLine("        new TypeConstraintDefinition();");
        }
        builder.AppendLine("}");
    }

}
