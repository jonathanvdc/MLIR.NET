namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class OperationEmitter
{
    public static void Emit(StringBuilder builder, OperationModel operation, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var plan = OperationMemberPlanner.Plan(operation, resolver);

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");

        OperationDefinitionEmitter.Emit(builder, className, operation, resolver);
        OperationPropertyEmitter.Emit(builder, className, operation, plan);
        OperationConstructorEmitter.Emit(builder, className, plan);

        builder.AppendLine("}");
    }
}
