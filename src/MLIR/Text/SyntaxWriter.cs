namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

/// <summary>
/// Provides token-aware, trivia-preserving writing services for MLIR syntax nodes.
/// </summary>
public sealed class SyntaxWriter
{
    private readonly StringBuilder builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxWriter"/> class.
    /// </summary>
    public SyntaxWriter()
        : this(new StringBuilder())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxWriter"/> class.
    /// </summary>
    /// <param name="builder">The underlying text builder to append to.</param>
    public SyntaxWriter(StringBuilder builder)
    {
        this.builder = builder;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return builder.ToString();
    }

    /// <summary>
    /// Appends raw text directly to the output.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void Write(string text)
    {
        builder.Append(text);
    }

    /// <summary>
    /// Writes a full module syntax tree.
    /// </summary>
    /// <param name="module">The module to write.</param>
    public void WriteModule(ModuleSyntax module)
    {
        module.WriteTo(this);
    }

    /// <summary>
    /// Writes an operation syntax node.
    /// </summary>
    /// <param name="operation">The operation to write.</param>
    /// <param name="indentLevel">The indentation level to use when indentation is synthesized.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia to use when syntax does not carry explicit trivia.</param>
    public void WriteOperation(OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        operation.WriteTo(this, indentLevel, defaultLeadingTrivia);
    }

    /// <summary>
    /// Writes a region syntax node.
    /// </summary>
    /// <param name="region">The region to write.</param>
    /// <param name="indentLevel">The indentation level of the containing operation.</param>
    public void WriteRegion(RegionSyntax region, int indentLevel)
    {
        region.WriteTo(this, indentLevel);
    }

    /// <summary>
    /// Writes a block syntax node.
    /// </summary>
    /// <param name="block">The block to write.</param>
    /// <param name="regionIndentLevel">The indentation level of the containing region.</param>
    public void WriteBlock(BlockSyntax block, int regionIndentLevel)
    {
        block.WriteTo(this, regionIndentLevel);
    }

    /// <summary>
    /// Writes a delimited list of syntax tokens.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited token list to write.</param>
    /// <param name="openLeadingTrivia">The fallback leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<SyntaxToken> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (token, writer, trivia) => writer.WriteToken(token, trivia));
    }

    /// <summary>
    /// Writes a delimited list of named attribute syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited attribute list to write.</param>
    /// <param name="openLeadingTrivia">The fallback leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<NamedAttributeSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (attr, writer, trivia) => attr.WriteTo(writer, trivia));
    }

    /// <summary>
    /// Writes a delimited list of block argument syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited block argument list to write.</param>
    /// <param name="openLeadingTrivia">The fallback leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<BlockArgumentSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (arg, writer, trivia) => arg.WriteTo(writer, trivia));
    }

    /// <summary>
    /// Writes a delimited list of type syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited type list to write.</param>
    /// <param name="openLeadingTrivia">The fallback leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<TypeSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (type, writer, trivia) => type.WriteTo(writer, trivia));
    }

    /// <summary>
    /// Writes a type syntax node.
    /// </summary>
    /// <param name="type">The type to write.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia.</param>
    public void WriteType(TypeSyntax type, string defaultLeadingTrivia)
    {
        type.WriteTo(this, defaultLeadingTrivia);
    }

    /// <summary>
    /// Writes a token using its preserved trivia when present, or synthesized trivia otherwise.
    /// </summary>
    /// <param name="token">The token to write.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia.</param>
    /// <param name="indentLevel">The indentation level to apply when trivia is synthesized.</param>
    public void WriteToken(SyntaxToken token, string defaultLeadingTrivia, int? indentLevel = null)
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
            WriteIndent(indentLevel.Value);
        }

        builder.Append(token.Text);
    }

    /// <summary>
    /// Writes preserved raw syntax text.
    /// </summary>
    /// <param name="rawText">The raw syntax text to write.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia.</param>
    public void WriteRaw(RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        builder.Append(rawText.HasLeadingTrivia ? rawText.LeadingTrivia : defaultLeadingTrivia);
        builder.Append(rawText.Text);
    }

    /// <summary>
    /// Writes indentation using the library's standard two-space indent width.
    /// </summary>
    /// <param name="indentLevel">The indentation level to write.</param>
    public void WriteIndent(int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }
}
