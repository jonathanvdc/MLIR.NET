namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

internal static class PrintWriter
{
    public static void AppendToken(StringBuilder builder, SyntaxToken token, string defaultLeadingTrivia, int? indentLevel = null)
    {
        if (token.LeadingTrivia.Length > 0)
        {
            builder.Append(token.LeadingTrivia);
            builder.Append(token.Text);
            return;
        }

        builder.Append(defaultLeadingTrivia);
        if (indentLevel.HasValue)
        {
            AppendIndent(builder, indentLevel.Value);
        }

        builder.Append(token.Text);
    }

    public static void AppendRaw(StringBuilder builder, RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        builder.Append(rawText.HasLeadingTrivia ? rawText.LeadingTrivia : defaultLeadingTrivia);
        builder.Append(rawText.Text);
    }

    public static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }

    public static void AppendBlockArgument(StringBuilder builder, BlockArgumentSyntax argument, string defaultLeadingTrivia)
    {
        AppendToken(builder, argument.NameToken, defaultLeadingTrivia);
        AppendToken(builder, argument.ColonToken, string.Empty);
        AppendRaw(builder, argument.Type, " ");
    }

    public static void AppendAttribute(StringBuilder builder, NamedAttributeSyntax attribute, string defaultLeadingTrivia)
    {
        AppendToken(builder, attribute.NameToken, defaultLeadingTrivia);
        AppendToken(builder, attribute.EqualsToken, " ");
        AppendRaw(builder, attribute.Value, " ");
    }
}
