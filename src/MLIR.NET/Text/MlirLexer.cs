namespace MLIR.Text;

using System;
using System.Collections.Generic;

internal static class MlirLexer
{
    public static IReadOnlyList<MlirToken> Lex(string source)
    {
        var tokens = new List<MlirToken>();
        var index = 0;
        var line = 1;
        var column = 1;

        while (index < source.Length)
        {
            var ch = source[index];
            if (ch == ' ' || ch == '\t' || ch == '\r')
            {
                Advance(ch, ref index, ref line, ref column);
                continue;
            }

            if (ch == '\n')
            {
                tokens.Add(new MlirToken(MlirTokenKind.NewLine, index, index + 1, line, column));
                Advance(ch, ref index, ref line, ref column);
                continue;
            }

            if (ch == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                while (index < source.Length && source[index] != '\n')
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                continue;
            }

            if (IsIdentifierStart(ch))
            {
                var start = index;
                var tokenLine = line;
                var tokenColumn = column;
                Advance(ch, ref index, ref line, ref column);

                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                tokens.Add(new MlirToken(MlirTokenKind.Identifier, start, index, tokenLine, tokenColumn));
                continue;
            }

            if (char.IsDigit(ch))
            {
                var start = index;
                var tokenLine = line;
                var tokenColumn = column;
                Advance(ch, ref index, ref line, ref column);

                while (index < source.Length && char.IsDigit(source[index]))
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                tokens.Add(new MlirToken(MlirTokenKind.Integer, start, index, tokenLine, tokenColumn));
                continue;
            }

            if (ch == '"')
            {
                var start = index;
                var tokenLine = line;
                var tokenColumn = column;
                Advance(ch, ref index, ref line, ref column);

                var escaped = false;
                while (index < source.Length)
                {
                    var current = source[index];
                    Advance(current, ref index, ref line, ref column);

                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (current == '"')
                    {
                        break;
                    }
                }

                if (source[index - 1] != '"')
                {
                    throw new MlirParseException(new MlirDiagnostic("Unterminated string literal.", tokenLine, tokenColumn));
                }

                tokens.Add(new MlirToken(MlirTokenKind.StringLiteral, start, index, tokenLine, tokenColumn));
                continue;
            }

            var punctuation = ch switch
            {
                '%' => MlirTokenKind.Percent,
                '^' => MlirTokenKind.Caret,
                '@' => MlirTokenKind.At,
                '#' => MlirTokenKind.Hash,
                ':' => MlirTokenKind.Colon,
                ',' => MlirTokenKind.Comma,
                '=' => MlirTokenKind.Equal,
                '(' => MlirTokenKind.LParen,
                ')' => MlirTokenKind.RParen,
                '{' => MlirTokenKind.LBrace,
                '}' => MlirTokenKind.RBrace,
                '[' => MlirTokenKind.LBracket,
                ']' => MlirTokenKind.RBracket,
                '<' => MlirTokenKind.LessThan,
                '>' => MlirTokenKind.GreaterThan,
                '?' => MlirTokenKind.Question,
                '*' => MlirTokenKind.Star,
                '+' => MlirTokenKind.Plus,
                '.' => MlirTokenKind.Dot,
                '-' => index + 1 < source.Length && source[index + 1] == '>' ? MlirTokenKind.Arrow : MlirTokenKind.Minus,
                _ => throw new MlirParseException(new MlirDiagnostic($"Unexpected character '{ch}'.", line, column)),
            };

            var tokenStart = index;
            var tokenLineValue = line;
            var tokenColumnValue = column;
            Advance(ch, ref index, ref line, ref column);

            if (punctuation == MlirTokenKind.Arrow)
            {
                Advance(source[index], ref index, ref line, ref column);
                tokens.Add(new MlirToken(MlirTokenKind.Arrow, tokenStart, index, tokenLineValue, tokenColumnValue));
            }
            else
            {
                tokens.Add(new MlirToken(punctuation, tokenStart, index, tokenLineValue, tokenColumnValue));
            }
        }

        tokens.Add(new MlirToken(MlirTokenKind.EndOfFile, source.Length, source.Length, line, column));
        return tokens;
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '$';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '$' || ch == '.' || ch == '-';
    }

    private static void Advance(char ch, ref int index, ref int line, ref int column)
    {
        index++;
        if (ch == '\n')
        {
            line++;
            column = 1;
        }
        else
        {
            column++;
        }
    }
}
