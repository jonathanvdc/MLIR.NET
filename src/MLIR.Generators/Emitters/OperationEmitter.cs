namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;

internal static class OperationEmitter
{
    public static void Emit(StringBuilder builder, OperationModel operation, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var plan = OperationMemberPlanner.Plan(operation, resolver);

        var hasSymbol = OperationPropertyEmitter.HasTrait(operation.Traits, "Symbol");
        var hasSymbolTable = OperationPropertyEmitter.HasTrait(operation.Traits, "SymbolTable");

        // Operations with the SymbolTable trait inherit from SymbolTableOperation, which provides
        // the O(1) cached symbol dictionary and invalidation logic.
        // Operations with the Symbol trait implement ISymbolOp to be discoverable via typed traversal.
        var baseClass = hasSymbolTable ? "SymbolTableOperation" : "Operation";
        var interfaces = hasSymbol ? ", ISymbolOp" : string.Empty;

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : " + baseClass + interfaces);
        builder.AppendLine("{");

        OperationDefinitionEmitter.Emit(builder, className, operation, resolver);
        OperationPropertyEmitter.Emit(builder, className, operation, plan);
        OperationConstructorEmitter.Emit(builder, className, plan);

        builder.AppendLine("}");
    }
}
