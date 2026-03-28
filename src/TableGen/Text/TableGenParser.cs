namespace TableGen.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TableGen.Syntax;

internal sealed class TableGenParser
{
    private readonly IReadOnlyList<TableGenToken> tokens;
    private int position;

    private TableGenParser(string source)
    {
        tokens = TableGenLexer.Lex(source);
    }

    public static TableGenDocumentSyntax ParseDocument(string source)
    {
        return new TableGenParser(source).ParseDocumentCore();
    }

    private TableGenDocumentSyntax ParseDocumentCore()
    {
        var declarations = new List<TableGenTopLevelSyntax>();
        while (!Is(TableGenTokenKind.EndOfFile))
        {
            declarations.Add(ParseTopLevel());
        }

        return new TableGenDocumentSyntax(declarations);
    }

    private TableGenTopLevelSyntax ParseTopLevel()
    {
        if (TryMatch(TableGenTokenKind.ClassKeyword))
        {
            return ParseClass();
        }

        if (TryMatch(TableGenTokenKind.DefKeyword))
        {
            return ParseDef();
        }

        throw Error("Expected 'class' or 'def'.");
    }

    private TableGenClassSyntax ParseClass()
    {
        var name = Expect(TableGenTokenKind.Identifier, "Expected a class name.").Text;
        var templateParameters = ParseOptionalTemplateParameters();
        var bases = ParseOptionalBases();
        var bodyItems = ParseOptionalBody();
        Expect(TableGenTokenKind.Semicolon, "Expected ';' after the class declaration.");
        return new TableGenClassSyntax(name, templateParameters, bases, bodyItems);
    }

    private TableGenDefSyntax ParseDef()
    {
        var name = Expect(TableGenTokenKind.Identifier, "Expected a definition name.").Text;
        var bases = ParseOptionalBases();
        var bodyItems = ParseOptionalBody();
        Expect(TableGenTokenKind.Semicolon, "Expected ';' after the definition.");
        return new TableGenDefSyntax(name, bases, bodyItems);
    }

    private IReadOnlyList<TableGenTemplateParameterSyntax> ParseOptionalTemplateParameters()
    {
        var parameters = new List<TableGenTemplateParameterSyntax>();
        if (!TryMatch(TableGenTokenKind.LessThan))
        {
            return parameters;
        }

        if (TryMatch(TableGenTokenKind.GreaterThan))
        {
            return parameters;
        }

        do
        {
            var typeName = ParseTypeName();
            var name = Expect(TableGenTokenKind.Identifier, "Expected a template parameter name.").Text;
            TableGenExpressionSyntax? defaultValue = null;
            if (TryMatch(TableGenTokenKind.Equal))
            {
                defaultValue = ParseExpression();
            }

            parameters.Add(new TableGenTemplateParameterSyntax(typeName, name, defaultValue));
        }
        while (TryMatch(TableGenTokenKind.Comma));

        Expect(TableGenTokenKind.GreaterThan, "Expected '>' to close the template parameter list.");
        return parameters;
    }

    private IReadOnlyList<TableGenBaseSyntax> ParseOptionalBases()
    {
        var bases = new List<TableGenBaseSyntax>();
        if (!TryMatch(TableGenTokenKind.Colon))
        {
            return bases;
        }

        do
        {
            var name = Expect(TableGenTokenKind.Identifier, "Expected a base-class name.").Text;
            bases.Add(new TableGenBaseSyntax(name, ParseOptionalArgumentList()));
        }
        while (TryMatch(TableGenTokenKind.Comma));

        return bases;
    }

    private IReadOnlyList<TableGenExpressionSyntax> ParseOptionalArgumentList()
    {
        var arguments = new List<TableGenExpressionSyntax>();
        if (!TryMatch(TableGenTokenKind.LessThan))
        {
            return arguments;
        }

        if (TryMatch(TableGenTokenKind.GreaterThan))
        {
            return arguments;
        }

        do
        {
            arguments.Add(ParseExpression());
        }
        while (TryMatch(TableGenTokenKind.Comma));

        Expect(TableGenTokenKind.GreaterThan, "Expected '>' to close the argument list.");
        return arguments;
    }

    private IReadOnlyList<TableGenBodyItemSyntax> ParseOptionalBody()
    {
        var items = new List<TableGenBodyItemSyntax>();
        if (!TryMatch(TableGenTokenKind.LBrace))
        {
            return items;
        }

        while (!TryMatch(TableGenTokenKind.RBrace))
        {
            items.Add(ParseBodyItem());
        }

        return items;
    }

    private TableGenBodyItemSyntax ParseBodyItem()
    {
        if (TryMatch(TableGenTokenKind.LetKeyword))
        {
            var name = Expect(TableGenTokenKind.Identifier, "Expected a field name after 'let'.").Text;
            Expect(TableGenTokenKind.Equal, "Expected '=' after the field name.");
            var value = ParseExpression();
            Expect(TableGenTokenKind.Semicolon, "Expected ';' after the let override.");
            return new TableGenLetSyntax(name, value);
        }

        var typeName = ParseTypeName();
        var nameToken = Expect(TableGenTokenKind.Identifier, "Expected a field name.");
        TableGenExpressionSyntax? initializer = null;
        if (TryMatch(TableGenTokenKind.Equal))
        {
            initializer = ParseExpression();
        }

        Expect(TableGenTokenKind.Semicolon, "Expected ';' after the field declaration.");
        return new TableGenFieldSyntax(typeName, nameToken.Text, initializer);
    }

    private string ParseTypeName()
    {
        var name = Expect(TableGenTokenKind.Identifier, "Expected a type name.").Text;
        if (!TryMatch(TableGenTokenKind.LessThan))
        {
            return name;
        }

        var parts = new List<string> { name, "<" };
        var depth = 1;
        while (depth > 0)
        {
            var token = Consume();
            switch (token.Kind)
            {
                case TableGenTokenKind.LessThan:
                    depth++;
                    break;
                case TableGenTokenKind.GreaterThan:
                    depth--;
                    break;
                case TableGenTokenKind.EndOfFile:
                    throw Error("Unexpected end of file while parsing a type argument list.");
            }

            parts.Add(token.Kind == TableGenTokenKind.String ? "\"" + token.Text + "\"" : token.Text);
        }

        return string.Concat(parts);
    }

    private TableGenExpressionSyntax ParseExpression()
    {
        if (TryMatch(TableGenTokenKind.Integer, out var integerToken))
        {
            return new TableGenIntegerSyntax(int.Parse(integerToken.Text, CultureInfo.InvariantCulture));
        }

        if (TryMatch(TableGenTokenKind.String, out var stringToken))
        {
            return new TableGenStringSyntax(stringToken.Text);
        }

        if (TryMatch(TableGenTokenKind.Identifier, out var identifierToken))
        {
            return new TableGenIdentifierSyntax(identifierToken.Text);
        }

        if (TryMatch(TableGenTokenKind.LBracket))
        {
            var items = new List<TableGenExpressionSyntax>();
            if (!TryMatch(TableGenTokenKind.RBracket))
            {
                do
                {
                    items.Add(ParseExpression());
                }
                while (TryMatch(TableGenTokenKind.Comma));

                Expect(TableGenTokenKind.RBracket, "Expected ']' to close the list literal.");
            }

            return new TableGenListSyntax(items);
        }

        throw Error("Expected an expression.");
    }

    private bool Is(TableGenTokenKind kind)
    {
        return Current.Kind == kind;
    }

    private TableGenToken Current => position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];

    private bool TryMatch(TableGenTokenKind kind)
    {
        if (!Is(kind))
        {
            return false;
        }

        position++;
        return true;
    }

    private bool TryMatch(TableGenTokenKind kind, out TableGenToken token)
    {
        if (!Is(kind))
        {
            token = default;
            return false;
        }

        token = tokens[position++];
        return true;
    }

    private TableGenToken Expect(TableGenTokenKind kind, string message)
    {
        if (!Is(kind))
        {
            throw Error(message);
        }

        return tokens[position++];
    }

    private TableGenToken Consume()
    {
        return position < tokens.Count ? tokens[position++] : tokens[tokens.Count - 1];
    }

    private TableGenParseException Error(string message)
    {
        var token = Current;
        return new TableGenParseException(new TableGenDiagnostic(message, token.Line, token.Column));
    }
}
