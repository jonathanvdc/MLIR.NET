namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class DialectRegistrationEmitter
{
    public static void Emit(StringBuilder builder, DialectModel dialect)
    {
        var dialectClassName = DialectGeneratorNaming.GetDialectRegistrationClassName(dialect);

        EmitterHelpers.AppendXmlDocComment(builder, dialect.Summary, dialect.Description);
        builder.AppendLine("public static class " + dialectClassName);
        builder.AppendLine("{");
        builder.AppendLine("    public static Dialect Create()");
        builder.AppendLine("    {");
        builder.AppendLine("        return Dialect.Create(\"" + dialect.Name + "\", dialect =>");
        builder.AppendLine("        {");

        foreach (var operation in dialect.Operations)
        {
            builder.AppendLine("            dialect.AddOperation(" + DialectGeneratorNaming.GetOperationClassName(operation) + ".OperationDefinition);");
        }

        foreach (var attribute in dialect.Attributes)
        {
            builder.AppendLine("            dialect.AddAttribute(" + DialectGeneratorNaming.GetAttributeClassName(attribute) + ".AttributeDefinition);");
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            builder.AppendLine("            dialect.AddAttributeConstraint(" + DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint) + ".AttributeConstraintDefinition);");
        }

        foreach (var type in dialect.Types)
        {
            builder.AppendLine("            dialect.AddType(" + DialectGeneratorNaming.GetTypeClassName(type) + ".TypeDefinition);");
        }

        foreach (var typeConstraint in dialect.TypeConstraints)
        {
            builder.AppendLine("            dialect.AddTypeConstraint(" + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + ".TypeConstraintDefinition);");
        }

        builder.Append("        }");
        if (!dialect.IsPrelude)
        {
            builder.Append(", global::MLIR.Dialects.Prelude.PreludeDialectRegistration.Create");
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
