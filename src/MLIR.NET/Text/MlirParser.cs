namespace MLIR.Text;

using System.Collections.Generic;
using System.Linq;
using MLIR.Syntax;

/// <summary>
/// Parses generic MLIR syntax into a structural syntax tree.
/// </summary>
public sealed class MlirParser
{
    private readonly string source;
    private readonly IReadOnlyList<MlirToken> tokens;
    private int position;

    private MlirParser(string source)
    {
        this.source = source;
        tokens = MlirLexer.Lex(source);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static MlirModuleSyntax ParseModule(string source)
    {
        return new MlirParser(source).ParseModuleCore();
    }

    private MlirModuleSyntax ParseModuleCore()
    {
        var operations = new List<OperationSyntax>();
        SkipBlankLines();
        while (!Is(MlirTokenKind.EndOfFile))
        {
            operations.Add(ParseOperation());
            ConsumeOperationTerminator();
            SkipBlankLines();
        }

        return new MlirModuleSyntax(operations);
    }

    private OperationSyntax ParseOperation()
    {
        var results = ParseResultList();
        var name = ParseOperationName();
        var operands = ParseOperandList();
        var successors = ParseSuccessors();
        var regions = ParseRegions();
        var attributes = ParseAttributeDictionary();
        var typeSignature = TryParseTypeSignature();

        return new OperationSyntax(results, name, operands, successors, regions, attributes, typeSignature);
    }

    private IReadOnlyList<string> ParseResultList()
    {
        if (!Is(MlirTokenKind.Percent))
        {
            return new List<string>();
        }

        var values = new List<string> { ParseSsaName() };
        while (TryMatch(MlirTokenKind.Comma))
        {
            values.Add(ParseSsaName());
        }

        Expect(MlirTokenKind.Equal, "Expected '=' after operation result list.");
        return values;
    }

    private string ParseOperationName()
    {
        if (Is(MlirTokenKind.StringLiteral))
        {
            return Slice(NextToken());
        }

        if (Is(MlirTokenKind.Identifier))
        {
            return Slice(NextToken());
        }

        throw Error("Expected an operation name.");
    }

    private IReadOnlyList<string> ParseOperandList()
    {
        Expect(MlirTokenKind.LParen, "Expected '(' to start the operand list.");
        var operands = new List<string>();
        if (TryMatch(MlirTokenKind.RParen))
        {
            return operands;
        }

        do
        {
            operands.Add(ParseSsaName());
        }
        while (TryMatch(MlirTokenKind.Comma));

        Expect(MlirTokenKind.RParen, "Expected ')' to close the operand list.");
        return operands;
    }

    private IReadOnlyList<string> ParseSuccessors()
    {
        var successors = new List<string>();
        if (!TryMatch(MlirTokenKind.LBracket))
        {
            return successors;
        }

        if (TryMatch(MlirTokenKind.RBracket))
        {
            return successors;
        }

        do
        {
            successors.Add(ParseBlockLabel());
        }
        while (TryMatch(MlirTokenKind.Comma));

        Expect(MlirTokenKind.RBracket, "Expected ']' to close the successor list.");
        return successors;
    }

    private IReadOnlyList<RegionSyntax> ParseRegions()
    {
        var regions = new List<RegionSyntax>();
        while (IsRegionStart())
        {
            regions.Add(ParseRegion());
        }

        return regions;
    }

    private RegionSyntax ParseRegion()
    {
        Expect(MlirTokenKind.LBrace, "Expected '{' to start a region.");
        SkipBlankLines();

        var blocks = new List<BlockSyntax>();
        var pendingEntryOperations = new List<OperationSyntax>();

        while (!Is(MlirTokenKind.RBrace))
        {
            if (Is(MlirTokenKind.Caret))
            {
                // MLIR allows the first block in a region to omit an explicit label.
                // We preserve those operations as a synthetic entry block in the syntax tree.
                if (pendingEntryOperations.Count > 0)
                {
                    blocks.Add(new BlockSyntax("^entry", new List<BlockArgumentSyntax>(), pendingEntryOperations.ToList()));
                    pendingEntryOperations.Clear();
                }

                blocks.Add(ParseBlock());
            }
            else
            {
                pendingEntryOperations.Add(ParseOperation());
                ConsumeOperationTerminator();
            }

            SkipBlankLines();
        }

        if (pendingEntryOperations.Count > 0 || blocks.Count == 0)
        {
            blocks.Insert(0, new BlockSyntax("^entry", new List<BlockArgumentSyntax>(), pendingEntryOperations.ToList()));
        }

        Expect(MlirTokenKind.RBrace, "Expected '}' to close a region.");
        return new RegionSyntax(blocks);
    }

    private BlockSyntax ParseBlock()
    {
        var label = ParseBlockLabel();
        var arguments = new List<BlockArgumentSyntax>();
        if (TryMatch(MlirTokenKind.LParen))
        {
            if (!TryMatch(MlirTokenKind.RParen))
            {
                do
                {
                    var name = ParseSsaName();
                    Expect(MlirTokenKind.Colon, "Expected ':' after block argument name.");
                    arguments.Add(new BlockArgumentSyntax(name, ParseRawUntilDelimiter(MlirTokenKind.Comma, MlirTokenKind.RParen)));
                }
                while (TryMatch(MlirTokenKind.Comma));

                Expect(MlirTokenKind.RParen, "Expected ')' after block argument list.");
            }
        }

        Expect(MlirTokenKind.Colon, "Expected ':' after block label.");
        ConsumeOptionalNewLine();

        var operations = new List<OperationSyntax>();
        SkipBlankLines();
        while (!Is(MlirTokenKind.RBrace) && !Is(MlirTokenKind.Caret))
        {
            operations.Add(ParseOperation());
            ConsumeOperationTerminator();
            SkipBlankLines();
        }

        return new BlockSyntax(label, arguments, operations);
    }

    private IReadOnlyList<NamedAttributeSyntax> ParseAttributeDictionary()
    {
        var attributes = new List<NamedAttributeSyntax>();
        if (!IsAttributeDictionaryStart())
        {
            return attributes;
        }

        Expect(MlirTokenKind.LBrace, "Expected '{' to start an attribute dictionary.");
        if (TryMatch(MlirTokenKind.RBrace))
        {
            return attributes;
        }

        do
        {
            var name = ParseAttributeName();
            Expect(MlirTokenKind.Equal, "Expected '=' after attribute name.");
            attributes.Add(new NamedAttributeSyntax(name, ParseRawUntilDelimiter(MlirTokenKind.Comma, MlirTokenKind.RBrace)));
        }
        while (TryMatch(MlirTokenKind.Comma));

        Expect(MlirTokenKind.RBrace, "Expected '}' to close the attribute dictionary.");
        return attributes;
    }

    private RawSyntaxText? TryParseTypeSignature()
    {
        if (!TryMatch(MlirTokenKind.Colon))
        {
            return null;
        }

        return ParseRawUntilOperationBoundary();
    }

    private RawSyntaxText ParseRawUntilDelimiter(params MlirTokenKind[] delimiters)
    {
        var start = Current.Start;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (true)
        {
            // Attribute values and types can contain nested delimiters of their own, so
            // the parser only stops when it returns to the outermost level.
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0 && delimiters.Contains(Current.Kind))
            {
                break;
            }

            if (Is(MlirTokenKind.EndOfFile))
            {
                throw Error("Unexpected end of file while parsing raw syntax.");
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            position++;
        }

        return new RawSyntaxText(source.Substring(start, tokens[position - 1].End - start).Trim());
    }

    private RawSyntaxText ParseRawUntilOperationBoundary()
    {
        var start = Current.Start;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (!Is(MlirTokenKind.EndOfFile))
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0 &&
                (Is(MlirTokenKind.NewLine) || Is(MlirTokenKind.RBrace)))
            {
                break;
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            position++;
        }

        var end = tokens[position - 1].End;
        return new RawSyntaxText(source.Substring(start, end - start).Trim());
    }

    private static void UpdateDepth(MlirTokenKind kind, ref int depthParen, ref int depthBrace, ref int depthBracket, ref int depthAngle)
    {
        switch (kind)
        {
            case MlirTokenKind.LParen:
                depthParen++;
                break;
            case MlirTokenKind.RParen:
                depthParen--;
                break;
            case MlirTokenKind.LBrace:
                depthBrace++;
                break;
            case MlirTokenKind.RBrace:
                depthBrace--;
                break;
            case MlirTokenKind.LBracket:
                depthBracket++;
                break;
            case MlirTokenKind.RBracket:
                depthBracket--;
                break;
            case MlirTokenKind.LessThan:
                depthAngle++;
                break;
            case MlirTokenKind.GreaterThan:
                depthAngle--;
                break;
        }
    }

    private string ParseAttributeName()
    {
        if (Is(MlirTokenKind.Identifier))
        {
            return Slice(NextToken());
        }

        if (Is(MlirTokenKind.StringLiteral))
        {
            return Slice(NextToken());
        }

        throw Error("Expected an attribute name.");
    }

    private string ParseSsaName()
    {
        Expect(MlirTokenKind.Percent, "Expected '%' to start an SSA value name.");
        if (!Is(MlirTokenKind.Identifier) && !Is(MlirTokenKind.Integer))
        {
            throw Error("Expected an SSA value name.");
        }

        return "%" + Slice(NextToken());
    }

    private string ParseBlockLabel()
    {
        Expect(MlirTokenKind.Caret, "Expected '^' to start a block label.");
        if (!Is(MlirTokenKind.Identifier) && !Is(MlirTokenKind.Integer))
        {
            throw Error("Expected a block label name.");
        }

        return "^" + Slice(NextToken());
    }

    private bool IsRegionStart()
    {
        if (!Is(MlirTokenKind.LBrace))
        {
            return false;
        }

        var lookahead = NextNonNewLineIndex(position + 1);
        if (tokens[lookahead].Kind == MlirTokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].Kind == MlirTokenKind.Caret || tokens[lookahead].Kind == MlirTokenKind.StringLiteral || tokens[lookahead].Kind == MlirTokenKind.Percent)
        {
            return true;
        }

        if (tokens[lookahead].Kind != MlirTokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = NextNonNewLineIndex(lookahead + 1);
        return tokens[secondLookahead].Kind != MlirTokenKind.Equal && tokens[secondLookahead].Kind != MlirTokenKind.Comma;
    }

    private bool IsAttributeDictionaryStart()
    {
        if (!Is(MlirTokenKind.LBrace))
        {
            return false;
        }

        var lookahead = NextNonNewLineIndex(position + 1);
        if (tokens[lookahead].Kind == MlirTokenKind.RBrace)
        {
            return true;
        }

        if (tokens[lookahead].Kind != MlirTokenKind.Identifier && tokens[lookahead].Kind != MlirTokenKind.StringLiteral)
        {
            return false;
        }

        var secondLookahead = NextNonNewLineIndex(lookahead + 1);
        return tokens[secondLookahead].Kind == MlirTokenKind.Equal || tokens[secondLookahead].Kind == MlirTokenKind.Comma || tokens[secondLookahead].Kind == MlirTokenKind.RBrace;
    }

    private int NextNonNewLineIndex(int startIndex)
    {
        var index = startIndex;
        while (tokens[index].Kind == MlirTokenKind.NewLine)
        {
            index++;
        }

        return index;
    }

    private void ConsumeOperationTerminator()
    {
        if (TryMatch(MlirTokenKind.NewLine))
        {
            return;
        }

        if (Is(MlirTokenKind.EndOfFile) || Is(MlirTokenKind.RBrace))
        {
            return;
        }

        throw Error("Expected the end of the operation.");
    }

    private void SkipBlankLines()
    {
        while (TryMatch(MlirTokenKind.NewLine))
        {
        }
    }

    private void ConsumeOptionalNewLine()
    {
        TryMatch(MlirTokenKind.NewLine);
    }

    private bool TryMatch(MlirTokenKind kind)
    {
        if (!Is(kind))
        {
            return false;
        }

        position++;
        return true;
    }

    private void Expect(MlirTokenKind kind, string message)
    {
        if (!TryMatch(kind))
        {
            throw Error(message);
        }
    }

    private bool Is(MlirTokenKind kind)
    {
        return Current.Kind == kind;
    }

    private MlirToken NextToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private MlirParseException Error(string message)
    {
        return new MlirParseException(new MlirDiagnostic(message, Current.Line, Current.Column));
    }

    private string Slice(MlirToken token)
    {
        return source.Substring(token.Start, token.End - token.Start);
    }

    private MlirToken Current => tokens[position];
}
