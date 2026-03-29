namespace MLIR.ODS;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Parses declarative MLIR ODS assembly format strings into <see cref="OdsAssemblyFormatModel"/> instances.
/// </summary>
public static class OdsAssemblyFormatParser
{
    /// <summary>
    /// Parses the given assembly format string.
    /// </summary>
    /// <param name="format">The assembly format string to parse.</param>
    /// <returns>The parsed <see cref="OdsAssemblyFormatModel"/>.</returns>
    /// <exception cref="FormatException">Thrown when the format string is malformed.</exception>
    public static OdsAssemblyFormatModel Parse(string format)
    {
        var parser = new Parser(format);
        return parser.ParseFormat();
    }

    private sealed class Parser
    {
        private readonly string _source;
        private int _pos;

        public Parser(string source)
        {
            _source = source;
            _pos = 0;
        }

        public OdsAssemblyFormatModel ParseFormat()
        {
            var elements = new List<Element>();
            SkipWhitespace();
            while (_pos < _source.Length)
            {
                elements.Add(ParseElement());
                SkipWhitespace();
            }
            return new OdsAssemblyFormatModel(elements);
        }

        // -----------------------------------------------------------------------
        // Elements
        // -----------------------------------------------------------------------

        private Element ParseElement()
        {
            if (Current == '(')
                return ParseOptionalGroup();
            return ParseChunk();
        }

        private OptionalGroup ParseOptionalGroup()
        {
            Expect('(');
            SkipWhitespace();

            var thenElements = new List<Element>();
            string? anchorName = null;

            while (_pos < _source.Length && Current != ')')
            {
                var element = ParseElement();
                SkipWhitespace();

                // The '^' anchor may follow a variable or a directive.
                if (element is VariableChunk vc && vc.IsAnchor)
                {
                    anchorName = vc.Name;
                }
                else if (_pos < _source.Length && Current == '^')
                {
                    Advance(); // consume '^'
                    anchorName = ExtractFirstVariableName(element);
                }

                thenElements.Add(element);
                SkipWhitespace();
            }

            Expect(')');
            SkipWhitespace();

            IReadOnlyList<Element>? elseElements = null;
            if (_pos < _source.Length && Current == ':')
            {
                Advance(); // consume ':'
                SkipWhitespace();
                Expect('(');
                SkipWhitespace();
                var elseList = new List<Element>();
                while (_pos < _source.Length && Current != ')')
                {
                    elseList.Add(ParseElement());
                    SkipWhitespace();
                }
                Expect(')');
                SkipWhitespace();
                elseElements = elseList;
            }

            Expect('?');

            return new OptionalGroup(anchorName ?? string.Empty, thenElements, elseElements);
        }

        // -----------------------------------------------------------------------
        // Chunks
        // -----------------------------------------------------------------------

        private Chunk ParseChunk()
        {
            if (Current == '`')
                return ParseLiteral();
            if (Current == '$')
                return ParseVariable();
            return ParseDirective();
        }

        private LiteralChunk ParseLiteral()
        {
            Expect('`');
            int start = _pos;
            while (_pos < _source.Length && Current != '`')
                Advance();
            string value = _source.Substring(start, _pos - start);
            Expect('`');
            return new LiteralChunk(value);
        }

        private VariableChunk ParseVariable()
        {
            Expect('$');
            string name = ParseIdentifier();
            bool isAnchor = false;
            if (_pos < _source.Length && Current == '^')
            {
                isAnchor = true;
                Advance();
            }
            return new VariableChunk(name, isAnchor);
        }

        private DirectiveChunk ParseDirective()
        {
            string name = ParseIdentifier();
            SkipWhitespace();

            switch (name)
            {
                case "attr-dict":
                    return new AttrDictDirectiveChunk();
                case "attr-dict-with-keyword":
                    return new AttrDictWithKeywordDirectiveChunk();
                case "prop-dict":
                    return new PropDictDirectiveChunk();
                case "operands":
                    return new OperandsDirectiveChunk();
                case "results":
                    return new ResultsDirectiveChunk();
                case "regions":
                    return new RegionsDirectiveChunk();
                case "successors":
                    return new SuccessorsDirectiveChunk();
                case "type":
                {
                    Expect('(');
                    SkipWhitespace();
                    var operand = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new TypeDirectiveChunk(operand);
                }
                case "functional-type":
                {
                    Expect('(');
                    SkipWhitespace();
                    var inputs = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(',');
                    SkipWhitespace();
                    var outputs = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new FunctionalTypeDirectiveChunk(inputs, outputs);
                }
                case "qualified":
                {
                    Expect('(');
                    SkipWhitespace();
                    var operand = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new QualifiedDirectiveChunk(operand);
                }
                case "ref":
                {
                    Expect('(');
                    SkipWhitespace();
                    var operand = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new RefDirectiveChunk(operand);
                }
                case "oilist":
                    return ParseOilistDirective();
                default:
                {
                    // Any other identifier followed by '(...)' is a custom directive.
                    Expect('(');
                    SkipWhitespace();
                    var parameters = new List<DirectiveOperand>();
                    if (_pos < _source.Length && Current != ')')
                    {
                        parameters.Add(ParseDirectiveOperand());
                        SkipWhitespace();
                        while (_pos < _source.Length && Current == ',')
                        {
                            Advance(); // consume ','
                            SkipWhitespace();
                            parameters.Add(ParseDirectiveOperand());
                            SkipWhitespace();
                        }
                    }
                    Expect(')');
                    return new CustomDirectiveChunk(name, parameters);
                }
            }
        }

        // -----------------------------------------------------------------------
        // oilist
        // -----------------------------------------------------------------------

        private OilistDirectiveChunk ParseOilistDirective()
        {
            Expect('(');
            SkipWhitespace();

            var clauses = new List<OilistClause>();
            clauses.Add(ParseOilistClause());
            SkipWhitespace();

            while (_pos < _source.Length && Current == '|')
            {
                Advance(); // consume '|'
                SkipWhitespace();
                clauses.Add(ParseOilistClause());
                SkipWhitespace();
            }

            Expect(')');
            return new OilistDirectiveChunk(clauses);
        }

        private OilistClause ParseOilistClause()
        {
            // Each clause starts with a backtick-delimited keyword.
            Expect('`');
            int start = _pos;
            while (_pos < _source.Length && Current != '`')
                Advance();
            string keyword = _source.Substring(start, _pos - start);
            Expect('`');
            SkipWhitespace();

            var elements = new List<OilistElement>();
            while (_pos < _source.Length && Current != '|' && Current != ')')
            {
                elements.Add(ParseOilistElement());
                SkipWhitespace();
            }

            return new OilistClause(keyword, elements);
        }

        private OilistElement ParseOilistElement()
        {
            if (Current == '`')
            {
                Expect('`');
                int start = _pos;
                while (_pos < _source.Length && Current != '`')
                    Advance();
                string value = _source.Substring(start, _pos - start);
                Expect('`');
                return new OilistLiteralElement(value);
            }

            if (Current == '$')
            {
                Advance(); // consume '$'
                string name = ParseIdentifier();
                return new OilistVariableElement(name);
            }

            // Remaining option is type(...).
            // oilist elements are restricted to literals, variables, and type directives per the ODS spec.
            string directiveName = ParseIdentifier();
            if (directiveName == "type")
            {
                SkipWhitespace();
                Expect('(');
                SkipWhitespace();
                var operand = ParseDirectiveOperand();
                SkipWhitespace();
                Expect(')');
                return new OilistTypeDirectiveElement(operand);
            }

            throw new FormatException(
                $"Unexpected oilist element '{directiveName}' at position {_pos}.");
        }

        // -----------------------------------------------------------------------
        // Directive operands
        // -----------------------------------------------------------------------

        private DirectiveOperand ParseDirectiveOperand()
        {
            if (_pos < _source.Length && Current == '$')
            {
                Advance(); // consume '$'
                string name = ParseIdentifier();
                return new VariableOperand(name);
            }

            string identifier = ParseIdentifier();
            SkipWhitespace();

            switch (identifier)
            {
                case "type":
                {
                    Expect('(');
                    SkipWhitespace();
                    var inner = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new TypeDirectiveOperand(inner);
                }
                case "ref":
                {
                    Expect('(');
                    SkipWhitespace();
                    var inner = ParseDirectiveOperand();
                    SkipWhitespace();
                    Expect(')');
                    return new RefDirectiveOperand(inner);
                }
                case "attr-dict":
                    return new AttrDictOperand();
                case "prop-dict":
                    return new PropDictOperand();
                case "results":
                case "operands":
                    // Bulk results/operands directive used as a functional-type parameter.
                    return new VariableOperand(identifier);
                default:
                    throw new FormatException(
                        $"Unexpected directive operand '{identifier}' at position {_pos}.");
            }
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Extracts the first variable name from a chunk, used when '^' follows a directive.
        /// </summary>
        private static string ExtractFirstVariableName(Element element)
        {
            if (element is VariableChunk vc)
                return vc.Name;

            // For single-operand directives, try to extract the variable name from the operand.
            DirectiveOperand? operand = element switch
            {
                TypeDirectiveChunk tdc => tdc.Operand,
                RefDirectiveChunk rdc => rdc.Operand,
                QualifiedDirectiveChunk qdc => qdc.Operand,
                _ => null,
            };

            if (operand is VariableOperand vo)
                return vo.Name;

            // Fall back to an empty string; the format is unusual.
            return string.Empty;
        }

        private string ParseIdentifier()
        {
            if (_pos >= _source.Length || !IsIdentifierStart(Current))
                throw new FormatException(
                    $"Expected identifier at position {_pos}, got '{(_pos < _source.Length ? Current : '\0')}'.");
            int start = _pos;
            while (_pos < _source.Length && IsIdentifierChar(Current))
                Advance();
            return _source.Substring(start, _pos - start);
        }

        private void SkipWhitespace()
        {
            while (_pos < _source.Length && char.IsWhiteSpace(Current))
                Advance();
        }

        private char Current => _source[_pos];

        private void Advance() => _pos++;

        private void Expect(char c)
        {
            if (_pos >= _source.Length || Current != c)
                throw new FormatException(
                    $"Expected '{c}' at position {_pos}, got '{(_pos < _source.Length ? Current : '\0')}'.");
            Advance();
        }

        private static bool IsIdentifierStart(char c) =>
            char.IsLetter(c) || c == '_';

        private static bool IsIdentifierChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '-';
    }
}
