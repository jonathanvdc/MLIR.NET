namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var strategy = resolver.TryResolveAttributeConstraintStrategy(attributeConstraint.RecordName);
        EmitStaticConstraintDefinition(builder, attributeConstraint, strategy);
        strategy.EmitAdditionalDefinitions(builder);
    }

    /// <summary>
    /// Emits a minimal static class that carries only the
    /// <c>AttributeConstraintDefinition</c> for constraints that bind to existing
    /// semantic storage types and do not need generated wrapper classes.
    /// </summary>
    private static void EmitStaticConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        AppendAttributeConstraintDefinition(builder, attributeConstraint, strategy, "        ");
        builder.AppendLine("}");
    }

    private static void AppendAttributeConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy,
        string indent)
    {
        AppendAttributeConstraintDefinition(
            builder,
            attributeConstraint.Name,
            GetAssemblyFormatExpression(strategy),
            indent);
    }

    private static void AppendAttributeConstraintDefinition(
        StringBuilder builder,
        string constraintName,
        string? assemblyFormatExpression,
        string indent)
    {
        builder.Append(indent);
        builder.Append("new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(constraintName));
        if (assemblyFormatExpression != null)
        {
            builder.Append(", ");
            builder.Append(assemblyFormatExpression);
        }

        builder.AppendLine(");");
    }

    private static string? GetAssemblyFormatExpression(AttributeConstraintCodeStrategy strategy)
    {
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression();
        if (assemblyFormatExpression != null)
        {
            return assemblyFormatExpression;
        }

        var assemblyFormatType = strategy.GetAssemblyFormatType();
        return assemblyFormatType != null ? "new " + assemblyFormatType + "()" : null;
    }
}
