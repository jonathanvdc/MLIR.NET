namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal abstract class AttrOrTypeSyntaxClassEmitter
{
    private readonly string syntaxClassName;
    private readonly IReadOnlyList<AssemblyFormatSyntaxField> fields;

    protected AttrOrTypeSyntaxClassEmitter(string syntaxClassName, IReadOnlyList<AssemblyFormatSyntaxField> fields)
    {
        this.syntaxClassName = syntaxClassName;
        this.fields = fields;
    }

    protected abstract string SyntaxBaseClass { get; }

    protected abstract string PrefixType { get; }

    protected abstract string SyntheticConstructorPrefixParameters { get; }

    protected abstract string SyntheticConstructorPrefixArgument { get; }

    protected abstract string RewritePrefixExpression { get; }

    protected abstract string GetLocationExpression(IReadOnlyList<VariableSyntaxField> variableFields);

    public void Emit(StringBuilder builder)
    {
        builder.AppendLine("public sealed class " + syntaxClassName + " : " + SyntaxBaseClass);
        builder.AppendLine("{");
        builder.AppendLine();

        EmitFullConstructor(builder);
        builder.AppendLine();
        EmitSyntheticConstructor(builder);
        EmitProperties(builder);
        EmitLocation(builder);
        EmitWriteTo(builder);
        EmitRewrite(builder);

        builder.AppendLine("}");
    }

    private void EmitFullConstructor(StringBuilder builder)
    {
        builder.Append("    public " + syntaxClassName + "(" + PrefixType + " prefix");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", Token " + lit.LocalName);
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + v.SyntaxType + " " + GetVariableSyntaxLocalName(v));
            }
        }

        builder.AppendLine(")");
        builder.AppendLine("        : base(prefix)");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.AppendLine("        " + GetLiteralPropertyName(lit) + " = " + lit.LocalName + ";");
            }
            else if (field is VariableSyntaxField v)
            {
                builder.AppendLine("        " + GetVariableSyntaxPropertyName(v) + " = " + GetVariableSyntaxLocalName(v) + ";");
            }
        }

        builder.AppendLine("    }");
    }

    private void EmitSyntheticConstructor(StringBuilder builder)
    {
        builder.Append("    public " + syntaxClassName + "(");
        var first = true;
        if (!string.IsNullOrEmpty(SyntheticConstructorPrefixParameters))
        {
            builder.Append(SyntheticConstructorPrefixParameters);
            first = false;
        }

        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(field.SyntaxType + " " + GetVariableSyntaxLocalName(field));
            first = false;
        }

        builder.AppendLine(")");
        builder.Append("        : this(" + SyntheticConstructorPrefixArgument);
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", " + GetSyntheticTokenExpression(lit));
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + GetVariableSyntaxLocalName(v));
            }
        }

        builder.AppendLine(") { }");
    }

    private void EmitProperties(StringBuilder builder)
    {
        var literalFields = fields.OfType<LiteralTokenField>().ToArray();
        if (literalFields.Length > 0)
        {
            builder.AppendLine();
            foreach (var field in literalFields)
            {
                builder.AppendLine("    public Token " + GetLiteralPropertyName(field) + " { get; }");
            }
        }

        var variableFields = fields.OfType<VariableSyntaxField>().ToArray();
        if (variableFields.Length > 0)
        {
            builder.AppendLine();
            foreach (var field in variableFields)
            {
                builder.AppendLine("    public " + field.SyntaxType + " " + GetVariableSyntaxPropertyName(field) + " { get; }");
            }
        }
    }

    private void EmitLocation(StringBuilder builder)
    {
        var variableFields = fields.OfType<VariableSyntaxField>().ToArray();
        builder.AppendLine();
        builder.AppendLine("    public override SourceLocation Location => " + GetLocationExpression(variableFields) + ";");
    }

    private void EmitWriteTo(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        builder.AppendLine("        WritePrefix(writer);");
        foreach (var field in fields)
        {
            switch (field)
            {
                case LiteralTokenField lit:
                    builder.AppendLine("        writer.WriteToken(" + GetLiteralPropertyName(lit) + ");");
                    break;

                case VariableSyntaxField v:
                    builder.AppendLine("        " + GetVariableSyntaxPropertyName(v) + ".WriteTo(writer);");
                    break;

                case TriviaSyntaxField trivia:
                    builder.AppendLine(trivia.IsNewline
                        ? "        writer.SuggestTrivia(\"\\n\");"
                        : "        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia.Text) + ");");
                    break;
            }
        }

        builder.AppendLine("    }");
    }

    private void EmitRewrite(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + syntaxClassName + "(" + RewritePrefixExpression);
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", rewriter.VisitToken(" + GetLiteralPropertyName(lit) + ")");
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + SyntaxValueShapeEmitter.GetRewriteExpression(v.Name, v.SyntaxType, v.SyntaxShape));
            }
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
    }

    protected static string GetLiteralPropertyName(LiteralTokenField field)
    {
        return EmitterHelpers.CapitalizeFirst(field.LocalName);
    }

    protected static string GetVariableSyntaxPropertyName(VariableSyntaxField field)
    {
        return DialectGeneratorNaming.ToPascalCase(field.Name) + "Syntax";
    }

    private static string GetVariableSyntaxLocalName(VariableSyntaxField field)
    {
        return EmitterHelpers.LowerFirst(field.Name) + "Syntax";
    }

    private static string GetSyntheticTokenExpression(LiteralTokenField field)
    {
        return field.IsKeyword
            ? "TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(field.SyntheticText) + ")"
            : "TokenFactory." + field.KindExpr.Substring("TokenKind.".Length) + "()";
    }
}

internal sealed class TypeSyntaxClassEmitter : AttrOrTypeSyntaxClassEmitter
{
    private readonly TypeModel type;

    public TypeSyntaxClassEmitter(TypeModel type, string syntaxClassName, IReadOnlyList<AssemblyFormatSyntaxField> fields)
        : base(syntaxClassName, fields)
    {
        this.type = type;
    }

    protected override string SyntaxBaseClass => "DialectNamedTypeSyntax";

    protected override string PrefixType => "DialectTypePrefix";

    protected override string SyntheticConstructorPrefixParameters => string.Empty;

    protected override string SyntheticConstructorPrefixArgument => "DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ")";

    protected override string RewritePrefixExpression => "new DialectTypePrefix(rewriter.VisitToken(Prefix.BangToken), rewriter.VisitToken(Prefix.NameToken))";

    protected override string GetLocationExpression(IReadOnlyList<VariableSyntaxField> variableFields)
    {
        return variableFields.Count > 0
            ? "SourceLocation.Merge(Prefix.Location, " + GetVariableSyntaxPropertyName(variableFields[0]) + ".Location)"
            : "Prefix.Location";
    }
}

internal sealed class AttributeSyntaxClassEmitter : AttrOrTypeSyntaxClassEmitter
{
    public AttributeSyntaxClassEmitter(string syntaxClassName, IReadOnlyList<AssemblyFormatSyntaxField> fields)
        : base(syntaxClassName, fields)
    {
    }

    protected override string SyntaxBaseClass => "DialectPrefixedAttributeValueSyntax";

    protected override string PrefixType => "DialectAttributePrefix";

    protected override string SyntheticConstructorPrefixParameters => "DialectAttributePrefix prefix";

    protected override string SyntheticConstructorPrefixArgument => "prefix";

    protected override string RewritePrefixExpression => "new DialectAttributePrefix(rewriter.VisitToken(Prefix.HashToken), rewriter.VisitToken(Prefix.NameToken))";

    protected override string GetLocationExpression(IReadOnlyList<VariableSyntaxField> variableFields)
    {
        return variableFields.Count > 0
            ? GetVariableSyntaxPropertyName(variableFields[0]) + ".Location"
            : "SourceLocation.Unknown";
    }
}
