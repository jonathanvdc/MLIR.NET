namespace MLIR.Generators.Emitters.AssemblyFormat;

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
        var bindBuildEmitter = new AttributeBindBuildEmitter(attribute, fields, className, syntaxClassName);

        builder.AppendLine("internal sealed class " + formatClassName + " : BodyOnlyAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatClassName + "()");
        builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        // TryParseBody
        builder.AppendLine("    protected override ParseResult<AttributeValueSyntax> TryParseBody(ParsingContext context, DialectAttributePrefix prefix)");
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
        bindBuildEmitter.EmitBindValueMethod(builder);
        builder.AppendLine();

        builder.AppendLine("    public override AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax, binder);");
        builder.AppendLine("    }");
        builder.AppendLine();

        // BuildCustomAssemblySyntax
        bindBuildEmitter.EmitBuildCustomAssemblySyntaxMethod(builder);

        builder.AppendLine("}");
    }
}
