namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Emits the generated concrete syntax class for a declarative type or attribute assembly format.
/// </summary>
/// <remarks>
/// Type and attribute formats both lower to an ordered list of syntax fields: literal tokens,
/// parameter syntax nodes, and, for attributes, preserved trivia. This base class owns the common
/// class shape built from those fields: constructors, token and parameter properties,
/// <c>Location</c>, <c>WriteTo</c>, and <c>Rewrite</c>. Subclasses provide the prefix details that
/// differ between <c>DialectTypePrefix</c> and <c>DialectAttributePrefix</c>.
/// </remarks>
internal abstract class AttrOrTypeSyntaxClassEmitter
{
    private readonly string syntaxClassName;
    private readonly IReadOnlyList<AssemblyFormatSyntaxField> fields;

    /// <summary>
    /// Creates an emitter for one generated syntax class.
    /// </summary>
    /// <param name="syntaxClassName">The generated class name, including the <c>Syntax</c> suffix.</param>
    /// <param name="fields">The lowered fields in declarative assembly-format order.</param>
    protected AttrOrTypeSyntaxClassEmitter(string syntaxClassName, IReadOnlyList<AssemblyFormatSyntaxField> fields)
    {
        this.syntaxClassName = syntaxClassName;
        this.fields = fields;
    }

    /// <summary>
    /// Gets the base syntax class used by the generated class.
    /// </summary>
    protected abstract string SyntaxBaseClass { get; }

    /// <summary>
    /// Gets the prefix type accepted by the full constructor.
    /// </summary>
    protected abstract string PrefixType { get; }

    /// <summary>
    /// Gets any prefix parameters needed by the synthetic convenience constructor.
    /// </summary>
    /// <remarks>
    /// Types synthesize their own prefix from the type name and therefore provide no extra
    /// parameter. Attributes keep the already-parsed dialect attribute prefix, because the
    /// body-only assembly-format base owns prefix parsing.
    /// </remarks>
    protected abstract string SyntheticConstructorPrefixParameters { get; }

    /// <summary>
    /// Gets the first argument passed from the synthetic constructor to the full constructor.
    /// </summary>
    protected abstract string SyntheticConstructorPrefixArgument { get; }

    /// <summary>
    /// Gets the prefix expression used when rewriting the generated syntax node.
    /// </summary>
    protected abstract string RewritePrefixExpression { get; }

    /// <summary>
    /// Builds the generated <c>Location</c> expression from the variable fields present in the
    /// syntax class.
    /// </summary>
    protected abstract string GetLocationExpression(IReadOnlyList<VariableSyntaxField> variableFields);

    /// <summary>
    /// Emits the full generated syntax class.
    /// </summary>
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

    /// <summary>
    /// Gets the property name used for a stored literal token field.
    /// </summary>
    protected static string GetLiteralPropertyName(LiteralTokenField field)
    {
        return EmitterHelpers.CapitalizeFirst(field.LocalName);
    }

    /// <summary>
    /// Gets the property name used for a variable's parsed syntax value.
    /// </summary>
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

/// <summary>
/// Emits syntax classes for generated type definitions with declarative assembly formats.
/// </summary>
internal sealed class TypeSyntaxClassEmitter : AttrOrTypeSyntaxClassEmitter
{
    private readonly TypeModel type;

    /// <summary>
    /// Creates an emitter for a generated type syntax class.
    /// </summary>
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

/// <summary>
/// Emits syntax classes for generated attribute definitions with declarative assembly formats.
/// </summary>
internal sealed class AttributeSyntaxClassEmitter : AttrOrTypeSyntaxClassEmitter
{
    /// <summary>
    /// Creates an emitter for a generated attribute syntax class.
    /// </summary>
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
