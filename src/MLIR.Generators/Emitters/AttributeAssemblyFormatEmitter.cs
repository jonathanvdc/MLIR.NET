namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Generates the structured <c>DialectPrefixedAttributeValueSyntax</c> subclass and the
/// <c>IBodyOnlyAttributeAssemblyFormat</c> implementation for an <c>AttrDef</c> with a
/// declarative <c>assemblyFormat</c> string.
/// </summary>
/// <remarks>
/// <para>
/// Two classes are emitted per parametrised attribute with a declarative format:
/// </para>
/// <list type="bullet">
///   <item>
///     <c>{ClassName}Syntax</c> — a sealed subclass of
///     <c>DialectPrefixedAttributeValueSyntax</c> that stores one typed property per
///     parameter and one <c>SyntaxToken</c> per literal element in the format.  Its
///     <c>WriteTo</c> method replays the stored tokens verbatim, preserving the source
///     form seen during parsing.  A synthetic convenience constructor is also emitted
///     that creates placeholder tokens from hard-coded format strings, so that callers
///     who construct the syntax programmatically do not need to supply raw tokens.
///   </item>
///   <item>
///     <c>{ClassName}AssemblyFormat</c> — a sealed implementation of
///     <c>IBodyOnlyAttributeAssemblyFormat</c> with <c>TryParse</c>, <c>Bind</c>, and
///     <c>BuildCustomAssemblySyntax</c> methods derived from the format elements.
///   </item>
/// </list>
/// </remarks>
internal static class AttributeAssemblyFormatEmitter
{
    // -----------------------------------------------------------------------
    // Public entry points
    // -----------------------------------------------------------------------

    /// <summary>
    /// Emits the structured <c>DialectPrefixedAttributeValueSyntax</c> subclass for the given
    /// attribute.  The class name is <c>{className}Syntax</c>.
    /// </summary>
    public static void EmitSyntaxClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var format = attribute.AssemblyFormat!;
        var syntaxClassName = className + "Syntax";
        var slots = BuildFormatSlots(attribute, format);

        builder.AppendLine("public sealed class " + syntaxClassName + " : DialectPrefixedAttributeValueSyntax");
        builder.AppendLine("{");
        builder.AppendLine();

        // Full constructor — takes prefix + all format elements in order (literals as SyntaxToken,
        // variables as their concrete csharpSyntaxType).  Used when constructing from parsed tokens
        // so that the exact source text is preserved.
        builder.Append("    public " + syntaxClassName + "(DialectAttributePrefix prefix");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", SyntaxToken " + lit.LocalName);
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + v.SyntaxType + " " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(")");
        builder.AppendLine("        : base(prefix)");
        builder.AppendLine("    {");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.AppendLine("        " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " = " + lit.LocalName + ";");
            }
            else if (slot is VariableSlot v)
            {
                builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax = " + EmitterHelpers.LowerFirst(v.Name) + "Syntax;");
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // Synthetic constructor — takes only prefix + variable syntaxes and creates placeholder
        // tokens for the literal elements.  Used in BuildCustomAssemblySyntax when constructing
        // a syntax node programmatically from typed attribute values.
        builder.Append("    public " + syntaxClassName + "(DialectAttributePrefix prefix");
        foreach (var slot in slots)
        {
            if (slot is VariableSlot v)
            {
                builder.Append(", " + v.SyntaxType + " " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(")");
        builder.Append("        : this(prefix");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", new SyntaxToken(" + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")");
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(") { }");

        // Literal token properties
        var hasLiterals = false;
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot)
            {
                hasLiterals = true;
                break;
            }
        }

        if (hasLiterals)
        {
            builder.AppendLine();
            foreach (var slot in slots)
            {
                if (slot is LiteralTokenSlot lit)
                {
                    builder.AppendLine("    public SyntaxToken " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " { get; }");
                }
            }
        }

        // Variable syntax properties
        var variableSlots = new List<VariableSlot>();
        foreach (var slot in slots)
        {
            if (slot is VariableSlot v)
            {
                variableSlots.Add(v);
            }
        }

        if (variableSlots.Count > 0)
        {
            builder.AppendLine();
            foreach (var v in variableSlots)
            {
                builder.AppendLine("    public " + v.SyntaxType + " " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax { get; }");
            }
        }

        // Location — delegate to first variable's syntax if present.
        builder.AppendLine();
        if (variableSlots.Count > 0)
        {
            builder.AppendLine("    public override SourceLocation Location => " + DialectGeneratorNaming.ToPascalCase(variableSlots[0].Name) + "Syntax.Location;");
        }
        else
        {
            builder.AppendLine("    public override SourceLocation Location => SourceLocation.Unknown;");
        }

        // WriteTo — prefix first, then stored tokens and sub-syntaxes in format order.
        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        builder.AppendLine("        WritePrefix(writer);");
        EmitWriteToBody(builder, slots);
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    /// <summary>
    /// Emits the <c>IBodyOnlyAttributeAssemblyFormat</c> implementation class for the given attribute.
    /// The class name is <c>{className}AssemblyFormat</c>.
    /// </summary>
    public static void EmitAssemblyFormatClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var format = attribute.AssemblyFormat!;
        var slots = BuildFormatSlots(attribute, format);
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        // IBodyOnlyAttributeAssemblyFormat signals to the parser that this format handles only
        // the body after '#dialect.attr'; the parser strips the prefix before calling TryParse.
        builder.AppendLine("internal sealed class " + formatClassName + " : IBodyOnlyAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine();

        // TryParse
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        EmitTryParseBody(builder, attribute, format, slots, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine();

        // Bind
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return definition.Factory(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));");
        builder.AppendLine("    }");
        builder.AppendLine();

        // BuildCustomAssemblySyntax
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder, attribute, slots, className, syntaxClassName);
        builder.AppendLine("    }");

        builder.AppendLine("}");
    }

    // -----------------------------------------------------------------------
    // WriteTo body
    // -----------------------------------------------------------------------

    /// <summary>
    /// Emits the body of <c>WriteTo</c> using the pre-built format slots.
    /// Each <see cref="LiteralTokenSlot"/> writes its stored token property;
    /// each <see cref="VariableSlot"/> calls <c>WriteTo</c> on its syntax property;
    /// trivia slots call <c>SuggestTrivia</c>.
    /// </summary>
    private static void EmitWriteToBody(StringBuilder builder, IReadOnlyList<FormatSlot> slots)
    {
        foreach (var slot in slots)
        {
            switch (slot)
            {
                case LiteralTokenSlot lit:
                    builder.AppendLine("        writer.WriteToken(" + EmitterHelpers.CapitalizeFirst(lit.LocalName) + ");");
                    break;

                case VariableSlot v:
                    builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax.WriteTo(writer);");
                    break;

                case TriviaSlot trivia:
                    if (trivia.IsNewline)
                    {
                        builder.AppendLine("        writer.SuggestTrivia(\"\\n\");");
                    }
                    else
                    {
                        builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia.Text) + ");");
                    }

                    break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // TryParse body
    // -----------------------------------------------------------------------

    private static void EmitTryParseBody(
        StringBuilder builder,
        AttributeModel attribute,
        AssemblyFormatModel format,
        IReadOnlyList<FormatSlot> slots,
        string syntaxClassName)
    {
        var elements = format.Elements;
        var isFirst = true;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            switch (slot)
            {
                case LiteralTokenSlot lit:
                    EmitLiteralTokenParse(builder, lit, ref isFirst);
                    break;

                case VariableSlot v:
                {
                    // For stop-token analysis we need the original element index; map via name.
                    var elementIndex = AssemblyFormatTraversal.FindElementIndexForVariable(elements, v.Name);
                    var stopTokens = AssemblyFormatTraversal.FindStopTokensForVariable(elements, elementIndex);
                    var varLocalName = EmitterHelpers.LowerFirst(v.Name) + "Syntax";
                    EmitVariableParse(builder, v.Name, varLocalName, stopTokens, v.ParamModel, v.SyntaxType);
                    isFirst = false;
                    break;
                }
            }
        }

        // Construct and return the syntax; pass prefix + all slots in order.
        builder.Append("        return ParseResult<AttributeValueSyntax>.Success(new " + syntaxClassName + "(context.Prefix ?? DialectAttributePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", " + lit.LocalName);
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine("));");
    }

    private static void EmitLiteralTokenParse(StringBuilder builder, LiteralTokenSlot lit, ref bool isFirst)
    {
        if (lit.IsKeyword)
        {
            var spellingExpr = EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText);
            if (isFirst)
            {
                builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var " + lit.LocalName + ") || " + lit.LocalName + ".Text != " + spellingExpr + ")");
                builder.AppendLine("        {");
                builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
                builder.AppendLine("        }");
                isFirst = false;
            }
            else
            {
                builder.AppendLine("        var " + lit.LocalName + "Result = context.Expect(TokenKind.Identifier, \"Expected keyword '" + EmitterHelpers.EscapeForStringLiteral(lit.SyntheticText, escapeSingleQuote: true) + "'.\");");
                builder.AppendLine("        if (!" + lit.LocalName + "Result.IsSuccess)");
                builder.AppendLine("            return ParseResult<AttributeValueSyntax>.Failure(" + lit.LocalName + "Result.Diagnostic!);");
                builder.AppendLine("        var " + lit.LocalName + " = " + lit.LocalName + "Result.Value;");
            }
        }
        else
        {
            if (isFirst)
            {
                builder.AppendLine("        if (!context.TryMatch(" + lit.KindExpr + ", out var " + lit.LocalName + "))");
                builder.AppendLine("        {");
                builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
                builder.AppendLine("        }");
                isFirst = false;
            }
            else
            {
                builder.AppendLine("        var " + lit.LocalName + "Result = context.Expect(" + lit.KindExpr + ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(lit.SyntheticText, escapeSingleQuote: true) + "'.\");");
                builder.AppendLine("        if (!" + lit.LocalName + "Result.IsSuccess)");
                builder.AppendLine("            return ParseResult<AttributeValueSyntax>.Failure(" + lit.LocalName + "Result.Diagnostic!);");
                builder.AppendLine("        var " + lit.LocalName + " = " + lit.LocalName + "Result.Value;");
            }
        }
    }

    private static void EmitVariableParse(
        StringBuilder builder,
        string variableName,
        string varLocalName,
        IReadOnlyList<TokenKind> stopTokens,
        AttrOrTypeParameterModel? paramModel,
        string syntaxType)
    {
        string parseExpr;

        if (!string.IsNullOrEmpty(paramModel?.CsharpParser))
        {
            // Custom parser from MLIRNet_AttrOrTypeParameterExtension.csharpParser:
            // substitute $_parser → context.
            parseExpr = paramModel!.CsharpParser!.Replace("$_parser", "context");
        }
        else
        {
            // Default: parse any attribute value, stopping before the inferred delimiters.
            var stopExpr = BuildStopTokensExpression(stopTokens);
            parseExpr = "context.TryParseAttributeValueSyntax(" + stopExpr + ")";
        }

        builder.AppendLine("        var " + varLocalName + "Result = " + parseExpr + ";");
        builder.AppendLine("        if (!" + varLocalName + "Result.IsSuccess)");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.Failure(" + varLocalName + "Result.Diagnostic!);");

        // Cast to concrete syntax type when the parameter has a specific syntax type.
        // This enables the syntax class constructor to accept a strongly-typed parameter.
        // The cast relies on the contract that the csharpParser expression declared in
        // MLIRNet_AttrOrTypeParameterExtension.csharpSyntaxType returns a value of exactly
        // the declared csharpSyntaxType.  For the built-in parameter types (StringRefParameter,
        // APIntParameter, APFloatParameter) this invariant is maintained by the helper methods
        // (TryParseStringLiteralSyntax, TryParseIntegerLiteralSyntax, etc.) on
        // AttributeParsingContext, which always return the appropriate concrete syntax class.
        // When adding a custom parameter type, the csharpParser expression must also satisfy
        // this contract.
        if (string.Equals(syntaxType, "AttributeValueSyntax", System.StringComparison.Ordinal))
        {
            builder.AppendLine("        var " + varLocalName + " = " + varLocalName + "Result.Value;");
        }
        else
        {
            builder.AppendLine("        var " + varLocalName + " = (" + syntaxType + ")" + varLocalName + "Result.Value;");
        }
    }

    // -----------------------------------------------------------------------
    // BuildCustomAssemblySyntax body
    // -----------------------------------------------------------------------

    private static void EmitBuildCustomAssemblySyntaxBody(
        StringBuilder builder,
        AttributeModel attribute,
        IReadOnlyList<FormatSlot> slots,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        var attr = (" + className + ")attribute;");
        // If the stored syntax is already the generated dialect syntax class, reuse it directly
        // so round-trip printing is allocation-free when nothing has changed.
        builder.AppendLine("        if (attr.Syntax is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        // For each variable slot, build the body syntax from the attribute's typed property.
        foreach (var slot in slots)
        {
            if (slot is VariableSlot v)
            {
                var propertyName = DialectGeneratorNaming.ToPascalCase(v.Name);
                var localSyntaxName = EmitterHelpers.LowerFirst(v.Name) + "Syntax";
                var buildExpr = BuildSyntaxFromPropertyExpression("attr." + propertyName, v.ParamModel);
                builder.AppendLine("        var " + localSyntaxName + " = " + buildExpr + ";");
            }
        }

        // Use a synthetic prefix so that WriteTo always outputs the '#dialect.attr' header
        // even when no real parse tokens are available.
        builder.Append("        return new " + syntaxClassName + "(DialectAttributePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
        foreach (var slot in slots)
        {
            if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(");");
    }

    /// <summary>
    /// Returns a C# expression that converts an attribute property value to
    /// an <c>AttributeValueSyntax</c> suitable for storage in the syntax class,
    /// using the parameter's <c>csharpPrinter</c> expression from the ODS model.
    /// </summary>
    private static string BuildSyntaxFromPropertyExpression(string propertyExpr, AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpPrinter))
        {
            // Custom printer from MLIRNet_AttrOrTypeParameterExtension.csharpPrinter:
            // substitute $_self → the property expression.
            return param!.CsharpPrinter!.Replace("$_self", propertyExpr);
        }

        // No printer defined: use the syntax node stored in the structured syntax class directly.
        // This is only valid when csharpType is AttributeValueSyntax.
        return propertyExpr;
    }

    // -----------------------------------------------------------------------
    // Stop-token analysis
    // -----------------------------------------------------------------------

    private static string BuildStopTokensExpression(IReadOnlyList<TokenKind> stopTokens)
    {
        if (stopTokens.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(stopTokens.Count);
        foreach (var kind in stopTokens)
        {
            parts.Add("TokenKind." + kind);
        }

        return string.Join(", ", parts);
    }

    // -----------------------------------------------------------------------
    // Format slots
    // -----------------------------------------------------------------------

    /// <summary>
    /// Abstract base for a single annotated slot in the ordered format element sequence.
    /// Slots interleave literal tokens, variable syntax references, and trivia hints,
    /// providing a unified view for all code-generation passes (constructor, WriteTo,
    /// TryParse, and BuildCustomAssemblySyntax).
    /// </summary>
    private abstract class FormatSlot { }

    /// <summary>
    /// Represents a storable literal token (punctuation or keyword) in the format.
    /// </summary>
    private sealed class LiteralTokenSlot : FormatSlot
    {
        /// <summary>Local variable / property name, e.g. <c>"literal0Token"</c>.</summary>
        public string LocalName { get; set; } = string.Empty;

        /// <summary>
        /// The text to use when constructing a synthetic token, e.g. <c>"&lt;"</c>.
        /// </summary>
        public string SyntheticText { get; set; } = string.Empty;

        /// <summary>
        /// The <c>TokenKind.Xxx</c> expression used in parse matching,
        /// e.g. <c>"TokenKind.LessThan"</c>.  Relevant only for non-keyword punctuation.
        /// </summary>
        public string KindExpr { get; set; } = string.Empty;

        /// <summary>True when this slot represents a keyword literal rather than punctuation.</summary>
        public bool IsKeyword { get; set; }
    }

    /// <summary>Represents a variable reference in the format (e.g. <c>$value</c>).</summary>
    private sealed class VariableSlot : FormatSlot
    {
        /// <summary>Variable name as declared in the format, e.g. <c>"value"</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// C# concrete syntax type for the generated property, e.g.
        /// <c>"StringAttributeValueSyntax"</c>.  Falls back to <c>"AttributeValueSyntax"</c>.
        /// </summary>
        public string SyntaxType { get; set; } = "AttributeValueSyntax";

        /// <summary>ODS parameter model for this variable, or null when not found.</summary>
        public AttrOrTypeParameterModel? ParamModel { get; set; }
    }

    /// <summary>
    /// Represents a trivia suggestion (whitespace or newline) that has no runtime token but
    /// guides the pretty-printer.
    /// </summary>
    private sealed class TriviaSlot : FormatSlot
    {
        /// <summary>Trivia text, e.g. <c>"  "</c>.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>True when this slot represents a newline rather than spaces.</summary>
        public bool IsNewline { get; set; }
    }

    /// <summary>
    /// Builds the unified ordered slot sequence for an attribute's assembly format.
    /// Each significant literal sub-element becomes a <see cref="LiteralTokenSlot"/>;
    /// each variable reference becomes a <see cref="VariableSlot"/>; whitespace/newline
    /// hints become <see cref="TriviaSlot"/>s.
    /// </summary>
    private static IReadOnlyList<FormatSlot> BuildFormatSlots(AttributeModel attribute, AssemblyFormatModel format)
    {
        var slots = new List<FormatSlot>();
        var literalIndex = 0;

        AssemblyFormatTraversal.VisitElements(
            format.Elements,
            onLiteral: literal =>
            {
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = EmitterHelpers.GetPunctuationText(punc.TokenKind),
                                KindExpr = "TokenKind." + punc.TokenKind,
                                IsKeyword = false,
                            });
                            literalIndex++;
                            break;

                        case KeywordLiteral kw:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = kw.Spelling,
                                KindExpr = "TokenKind.Identifier",
                                IsKeyword = true,
                            });
                            literalIndex++;
                            break;

                        case WhitespaceLiteral ws:
                            slots.Add(new TriviaSlot { Text = ws.Spaces, IsNewline = false });
                            break;

                        case NewlineLiteral:
                            slots.Add(new TriviaSlot { Text = "\n", IsNewline = true });
                            break;

                        // EmptyLiteral: no slot
                    }
                }
            },
            onVariable: variable =>
            {
                var paramModel = FindParameter(attribute, variable.Name);
                slots.Add(new VariableSlot
                {
                    Name = variable.Name,
                    SyntaxType = GetResolvedCSharpSyntaxType(paramModel),
                    ParamModel = paramModel,
                });
            });

        return slots;
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    /// <summary>
    /// Finds the parameter model for the given variable name, or null when not found.
    /// </summary>
    internal static AttrOrTypeParameterModel? FindParameter(AttributeModel attribute, string variableName)
    {
        foreach (var param in attribute.Parameters)
        {
            if (string.Equals(param.Name, variableName, System.StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the C# type that should be used for the given parameter's typed property.
    /// Falls back to <c>"AttributeValueSyntax"</c> when no better mapping is known.
    /// </summary>
    internal static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
    {
        if (param == null)
        {
            return "AttributeValueSyntax";
        }

        if (!string.IsNullOrEmpty(param.CsharpType))
        {
            return param.CsharpType!;
        }

        // Fall back to AttributeValueSyntax for parameters without a known C# mapping.
        return "AttributeValueSyntax";
    }

    /// <summary>
    /// Returns the C# concrete syntax type for the generated per-parameter syntax property.
    /// Comes from <c>MLIRNet_AttrOrTypeParameterExtension.csharpSyntaxType</c> when set;
    /// falls back to <c>"AttributeValueSyntax"</c> otherwise.
    /// </summary>
    private static string GetResolvedCSharpSyntaxType(AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpSyntaxType))
        {
            return param!.CsharpSyntaxType!;
        }

        return "AttributeValueSyntax";
    }

}
