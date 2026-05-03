namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal abstract class AttrOrTypeBindBuildEmitter
{
    private readonly IReadOnlyList<AssemblyFormatSyntaxField> fields;
    private readonly string className;
    private readonly string syntaxClassName;

    protected AttrOrTypeBindBuildEmitter(
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
    {
        this.fields = fields;
        this.className = className;
        this.syntaxClassName = syntaxClassName;
    }

    protected abstract string SemanticKind { get; }

    protected abstract string SemanticValueType { get; }

    protected abstract string SyntaxValueType { get; }

    protected abstract string SemanticParameterName { get; }

    protected abstract string TypedLocalName { get; }

    protected abstract string OwnerName { get; }

    protected abstract string ExistingSyntaxExpression { get; }

    protected abstract string SyntheticPrefixExpression { get; }

    protected virtual bool IncludeLiteralTokenArgumentsInBuild => true;

    public void EmitBindValueMethod(StringBuilder builder)
    {
        builder.AppendLine("    public static " + SemanticValueType + " BindValue(" + SyntaxValueType + " syntax, Binder binder)");
        builder.AppendLine("    {");
        EmitBindValueBody(builder);
        builder.AppendLine("    }");
    }

    public void EmitBuildCustomAssemblySyntaxMethod(StringBuilder builder)
    {
        builder.AppendLine("    public " + BuildCustomAssemblySyntaxOverrideModifier + SyntaxValueType + " BuildCustomAssemblySyntax(" + SemanticValueType + " " + SemanticParameterName + ", ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder);
        builder.AppendLine("    }");
    }

    protected virtual string BuildCustomAssemblySyntaxOverrideModifier => string.Empty;

    protected virtual string BuildValueFromSyntaxExpression(
        AttrOrTypeParameterModel? param,
        string syntaxExpr,
        string parameterName)
    {
        var extractorTemplate = param?.CsharpExtractorTemplate;
        if (extractorTemplate is not null)
        {
            return extractorTemplate.Render("syntax", syntaxExpr);
        }

        if (!string.IsNullOrEmpty(param?.CsharpDefault))
        {
            return param!.CsharpDefault!;
        }

        var message = "Missing syntax for parameter '" + parameterName + "' on " + SemanticKind + " '" + OwnerName + "' and no C# extractor/default was defined.";
        return "throw new global::System.InvalidOperationException(" + EmitterHelpers.ToCSharpStringLiteral(message) + ")";
    }

    private void EmitBuildCustomAssemblySyntaxBody(StringBuilder builder)
    {
        builder.AppendLine("        var " + TypedLocalName + " = (" + className + ")" + SemanticParameterName + ";");
        builder.AppendLine("        if (" + ExistingSyntaxExpression + " is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(field.Name);
            var localSyntaxName = EmitterHelpers.LowerFirst(field.Name) + "Syntax";
            var buildExpr = BuildSyntaxFromPropertyExpression(TypedLocalName + "." + propertyName, field.ParamModel);
            builder.AppendLine("        var " + localSyntaxName + " = " + buildExpr + ";");
        }

        builder.Append("        return new " + syntaxClassName + "(" + SyntheticPrefixExpression);
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                if (IncludeLiteralTokenArgumentsInBuild)
                {
                    builder.Append(", " + GetSyntheticTokenExpression(lit));
                }
            }
            else if (field is VariableSyntaxField variable)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(variable.Name) + "Syntax");
            }
        }

        builder.AppendLine(");");
    }

    private void EmitBindValueBody(StringBuilder builder)
    {
        builder.AppendLine("        if (syntax is not " + syntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated " + SemanticKind + " syntax class.\");");

        var constructorArguments = new List<string>();
        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(field.Name);
            var localName = EmitterHelpers.LowerFirst(field.Name) + "Value";
            var syntaxExpr = "structured." + propertyName + "Syntax";
            var valueExpr = BuildValueFromSyntaxExpression(field.ParamModel, syntaxExpr, field.Name);
            builder.AppendLine("        var " + localName + " = " + valueExpr + ";");
            constructorArguments.Add(localName);
        }

        builder.AppendLine("        return new " + className + "(" + string.Join(", ", constructorArguments) + ", syntax);");
    }

    private static string BuildSyntaxFromPropertyExpression(string propertyExpr, AttrOrTypeParameterModel? param)
    {
        var printerTemplate = param?.CsharpPrinterTemplate;
        if (printerTemplate is not null)
        {
            return printerTemplate.Render("self", propertyExpr);
        }

        return propertyExpr;
    }

    private static string GetSyntheticTokenExpression(LiteralTokenField field)
    {
        return field.IsKeyword
            ? "TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(field.SyntheticText) + ")"
            : "TokenFactory." + field.KindExpr.Substring("TokenKind.".Length) + "()";
    }
}

internal sealed class TypeBindBuildEmitter : AttrOrTypeBindBuildEmitter
{
    public TypeBindBuildEmitter(
        TypeModel type,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
        : base(fields, className, syntaxClassName)
    {
        Type = type;
    }

    private TypeModel Type { get; }

    protected override string SemanticKind => "type";

    protected override string SemanticValueType => "TypeReference";

    protected override string SyntaxValueType => "TypeSyntax";

    protected override string SemanticParameterName => "type";

    protected override string TypedLocalName => "typed";

    protected override string OwnerName => Type.Name;

    protected override string ExistingSyntaxExpression => "typed.Syntax";

    protected override string SyntheticPrefixExpression => "DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(Type.Name ?? string.Empty) + ")";
}

internal sealed class AttributeBindBuildEmitter : AttrOrTypeBindBuildEmitter
{
    public AttributeBindBuildEmitter(
        AttributeModel attribute,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
        : base(fields, className, syntaxClassName)
    {
        Attribute = attribute;
    }

    private AttributeModel Attribute { get; }

    protected override string SemanticKind => "attribute";

    protected override string SemanticValueType => "AttributeValue";

    protected override string SyntaxValueType => "AttributeValueSyntax";

    protected override string SemanticParameterName => "attribute";

    protected override string TypedLocalName => "attr";

    protected override string OwnerName => Attribute.Name;

    protected override string ExistingSyntaxExpression => "attr.Syntax";

    protected override string SyntheticPrefixExpression => "DialectAttributePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(Attribute.Name) + ")";

    protected override bool IncludeLiteralTokenArgumentsInBuild => false;

    protected override string BuildCustomAssemblySyntaxOverrideModifier => "override ";

    protected override string BuildValueFromSyntaxExpression(
        AttrOrTypeParameterModel? param,
        string syntaxExpr,
        string parameterName)
    {
        return param?.IsSelfTypeParameter == true
            ? "binder.BindTypeReference(" + syntaxExpr + ".TypeSyntax)"
            : base.BuildValueFromSyntaxExpression(param, syntaxExpr, parameterName);
    }
}
