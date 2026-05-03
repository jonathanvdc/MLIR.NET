namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Linq;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.ODS.Model;
using MLIR.Generators.Emitters.Common;

/// <summary>
/// Generates the structured <c>DialectPrefixedAttributeValueSyntax</c> subclass and the
/// <c>BodyOnlyAttributeAssemblyFormat</c> implementation for an <c>AttrDef</c> with a
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
///     parameter and one <c>Token</c> per literal element in the format.  Its
///     <c>WriteTo</c> method replays the stored tokens verbatim, preserving the source
///     form seen during parsing.  A synthetic convenience constructor is also emitted
///     that creates placeholder tokens from hard-coded format strings, so that callers
///     who construct the syntax programmatically do not need to supply raw tokens.
///   </item>
///   <item>
///     <c>{ClassName}AssemblyFormat</c> — a sealed implementation of
///     <c>BodyOnlyAttributeAssemblyFormat</c> with <c>TryParseBody</c>, <c>Bind</c>, and
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
        var lowered = AssemblyFormatLowerer.LowerAttribute(attribute, format);
        new AttributeSyntaxClassEmitter(syntaxClassName, lowered.Fields).Emit(builder);
    }

    /// <summary>
    /// Emits the <c>BodyOnlyAttributeAssemblyFormat</c> implementation class for the given attribute.
    /// The class name is <c>{className}AssemblyFormat</c>.
    /// </summary>
    public static void EmitAssemblyFormatClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var format = attribute.AssemblyFormat!;
        var lowered = AssemblyFormatLowerer.LowerAttribute(attribute, format);
        var fields = lowered.Fields;
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("internal sealed class " + formatClassName + " : BodyOnlyAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatClassName + "()");
        builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        // TryParseBody
        builder.AppendLine("    protected override ParseResult<AttributeValueSyntax> TryParseBody(AttributeParsingContext context, DialectAttributePrefix prefix)");
        builder.AppendLine("    {");
        if (!lowered.IsSupported)
        {
            builder.AppendLine("        return ParseResult<AttributeValueSyntax>.Failure(new AssemblyDiagnostic(prefix.Location, \"Unsupported declarative assembly format construct for attribute body.\"));");
        }
        else
        {
            new AttributeTryParseBodyEmitter(lowered, syntaxClassName).Emit(builder);
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // Bind
        builder.AppendLine("    public static AttributeValue BindValue(AttributeValueSyntax syntax, Binder binder)");
        builder.AppendLine("    {");
        EmitBindValueBody(builder, attribute, className, fields, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    public override AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax, binder);");
        builder.AppendLine("    }");
        builder.AppendLine();

        // BuildCustomAssemblySyntax
        builder.AppendLine("    public override AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder, attribute, fields, className, syntaxClassName);
        builder.AppendLine("    }");

        builder.AppendLine("}");
    }

    // -----------------------------------------------------------------------
    // BuildCustomAssemblySyntax body
    // -----------------------------------------------------------------------

    private static void EmitBuildCustomAssemblySyntaxBody(
        StringBuilder builder,
        AttributeModel attribute,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        var attr = (" + className + ")attribute;");
        // If the stored syntax is already the generated dialect syntax class, reuse it directly
        // so round-trip printing is allocation-free when nothing has changed.
        builder.AppendLine("        if (attr.Syntax is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        // For each variable field, build the body syntax from the attribute's typed property.
        foreach (var field in fields)
        {
            if (field is VariableSyntaxField v)
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
        foreach (var field in fields)
        {
            if (field is VariableSyntaxField v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(");");
    }

    private static void EmitBindValueBody(
        StringBuilder builder,
        AttributeModel attribute,
        string className,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string syntaxClassName)
    {
        builder.AppendLine("        if (syntax is not " + syntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated attribute syntax class.\");");

        var constructorArguments = new List<string>();
        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(field.Name);
            var localName = EmitterHelpers.LowerFirst(field.Name) + "Value";
            var syntaxExpr = "structured." + propertyName + "Syntax";
            var valueExpr = BuildValueFromSyntaxExpression(field.ParamModel, syntaxExpr, attribute.Name, field.Name);
            builder.AppendLine("        var " + localName + " = " + valueExpr + ";");
            constructorArguments.Add(localName);
        }

        builder.AppendLine("        return new " + className + "(" + string.Join(", ", constructorArguments) + ", syntax);");
    }

    private static string BuildValueFromSyntaxExpression(
        AttrOrTypeParameterModel? param,
        string syntaxExpr,
        string ownerName,
        string parameterName)
    {
        if (param?.IsSelfTypeParameter == true)
        {
            return "binder.BindTypeReference(" + syntaxExpr + ".TypeSyntax)";
        }

        var extractorTemplate = param?.CsharpExtractorTemplate;
        if (extractorTemplate is not null)
        {
            return extractorTemplate.Render("syntax", syntaxExpr);
        }

        if (!string.IsNullOrEmpty(param?.CsharpDefault))
        {
            return param!.CsharpDefault!;
        }

        var message = "Missing syntax for parameter '" + parameterName + "' on attribute '" + ownerName + "' and no C# extractor/default was defined.";
        return "throw new global::System.InvalidOperationException(" + EmitterHelpers.ToCSharpStringLiteral(message) + ")";
    }

    /// <summary>
    /// Returns a C# expression that converts an attribute property value to
    /// an <c>AttributeValueSyntax</c> suitable for storage in the syntax class,
    /// using the parameter's <c>csharpPrinter</c> expression from the ODS model.
    /// </summary>
    private static string BuildSyntaxFromPropertyExpression(string propertyExpr, AttrOrTypeParameterModel? param)
    {
        var printerTemplate = param?.CsharpPrinterTemplate;
        if (printerTemplate is not null)
        {
            // Custom printer from CSharpParameterExtension.csharpPrinter:
            // substitute ${self} → the property expression.
            return printerTemplate.Render("self", propertyExpr);
        }

        // No printer defined: use the syntax node stored in the structured syntax class directly.
        // This is only valid when csharpType is AttributeValueSyntax.
        return propertyExpr;
    }

}
