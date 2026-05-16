namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using MLIR.ODS.Model;

/// <summary>
/// Compiles declarative assembly formats through a single subject-neutral plan.
/// Subject adapters resolve what variables mean for operations, attributes, and types; the
/// parser/binder/builder emission then walks the same slot list for every subject kind.
/// </summary>
internal static class UnifiedAssemblyFormatEmitter
{
    public static void EmitAttribute(StringBuilder builder, AttributeModel attribute, string className, IList<Diagnostic> diagnostics)
    {
        var subject = new AttributeFormatSubject(attribute, className);
        Emit(builder, subject, diagnostics);
    }

    public static void EmitType(StringBuilder builder, TypeModel type, string className, IList<Diagnostic> diagnostics)
    {
        var subject = new TypeFormatSubject(type, className);
        Emit(builder, subject, diagnostics);
    }

    public static void EmitOperation(StringBuilder builder, OperationModel operation, string className, DialectSymbolResolver resolver, IList<Diagnostic> diagnostics)
    {
        var subject = new OperationFormatSubject(operation, className, resolver);
        Emit(builder, subject, diagnostics);
    }

    public static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
        => !string.IsNullOrEmpty(param?.CsharpType) ? param!.CsharpType! : "AttributeValueSyntax";

    private static void Emit(StringBuilder builder, FormatSubject subject, IList<Diagnostic> diagnostics)
    {
        var plan = Compile(subject, diagnostics);
        AssemblyFormatSyntaxClassEmitter.Emit(builder, subject, plan);
        builder.AppendLine();
        AssemblyFormatClassEmitter.Emit(builder, subject, plan);
    }

    private static AssemblyFormatPlan Compile(FormatSubject subject, IList<Diagnostic> diagnostics)
    {
        var compiler = new AssemblyFormatPlanCompiler(subject);
        var plan = compiler.Compile();
        foreach (var unsupported in plan.UnsupportedFeatures)
        {
            diagnostics.Add(Diagnostic.Create(
                DialectGeneratorDiagnostics.UnsupportedAssemblyFormatFeature,
                Location.None,
                subject.SubjectKind,
                subject.DisplayName,
                unsupported));
        }

        return plan;
    }
}
