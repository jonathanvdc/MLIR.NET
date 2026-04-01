namespace TableGen.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TableGen.Syntax;

internal sealed class Parser
{
    private readonly IReadOnlyList<Token> tokens;
    private int position;

    private Parser(string source)
    {
        tokens = Lexer.Lex(source);
    }

    public static DocumentSyntax ParseDocument(string source)
    {
        return new Parser(source).ParseDocumentCore();
    }

    private DocumentSyntax ParseDocumentCore()
    {
        var declarations = new List<TopLevelSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            declarations.Add(ParseTopLevel());
        }

        return new DocumentSyntax(declarations);
    }

    private TopLevelSyntax ParseTopLevel()
    {
        if (TryMatch(TokenKind.ClassKeyword))
        {
            return ParseClass();
        }

        if (TryMatch(TokenKind.DefKeyword))
        {
            return ParseDef();
        }

        throw Error("Expected 'class' or 'def'.");
    }

    private ClassSyntax ParseClass()
    {
        var name = Expect(TokenKind.Identifier, "Expected a class name.").Text;
        var templateParameters = ParseOptionalTemplateParameters();
        var bases = ParseOptionalBases();
        var (hadBraces, bodyItems) = ParseOptionalBody();
        if (!hadBraces || Is(TokenKind.Semicolon))
        {
            Expect(TokenKind.Semicolon, "Expected ';' after the class declaration.");
        }

        return new ClassSyntax(name, templateParameters, bases, bodyItems);
    }

    private DefSyntax ParseDef()
    {
        var name = Expect(TokenKind.Identifier, "Expected a definition name.").Text;
        var bases = ParseOptionalBases();
        var (hadBraces, bodyItems) = ParseOptionalBody();
        if (!hadBraces || Is(TokenKind.Semicolon))
        {
            Expect(TokenKind.Semicolon, "Expected ';' after the definition.");
        }

        return new DefSyntax(name, bases, bodyItems);
    }

    private IReadOnlyList<TemplateParameterSyntax> ParseOptionalTemplateParameters()
    {
        var parameters = new List<TemplateParameterSyntax>();
        if (!TryMatch(TokenKind.LessThan))
        {
            return parameters;
        }

        if (TryMatch(TokenKind.GreaterThan))
        {
            return parameters;
        }

        do
        {
            var typeName = ParseTypeName();
            var name = Expect(TokenKind.Identifier, "Expected a template parameter name.").Text;
            ExpressionSyntax? defaultValue = null;
            if (TryMatch(TokenKind.Equal))
            {
                defaultValue = ParseExpression();
            }

            parameters.Add(new TemplateParameterSyntax(typeName, name, defaultValue));
        }
        while (TryMatch(TokenKind.Comma));

        Expect(TokenKind.GreaterThan, "Expected '>' to close the template parameter list.");
        return parameters;
    }

    private IReadOnlyList<BaseSyntax> ParseOptionalBases()
    {
        var bases = new List<BaseSyntax>();
        if (!TryMatch(TokenKind.Colon))
        {
            return bases;
        }

        do
        {
            var name = Expect(TokenKind.Identifier, "Expected a base-class name.").Text;
            bases.Add(new BaseSyntax(name, ParseOptionalArgumentList()));
        }
        while (TryMatch(TokenKind.Comma));

        return bases;
    }

    private IReadOnlyList<ExpressionSyntax> ParseOptionalArgumentList()
    {
        var arguments = new List<ExpressionSyntax>();
        if (!TryMatch(TokenKind.LessThan))
        {
            return arguments;
        }

        if (TryMatch(TokenKind.GreaterThan))
        {
            return arguments;
        }

        do
        {
            arguments.Add(ParseExpression());
        }
        while (TryMatch(TokenKind.Comma));

        Expect(TokenKind.GreaterThan, "Expected '>' to close the argument list.");
        return arguments;
    }

    private (bool hadBraces, IReadOnlyList<BodyItemSyntax> items) ParseOptionalBody()
    {
        var items = new List<BodyItemSyntax>();
        if (!TryMatch(TokenKind.LBrace))
        {
            return (false, items);
        }

        while (!TryMatch(TokenKind.RBrace))
        {
            items.Add(ParseBodyItem());
        }

        return (true, items);
    }

    private BodyItemSyntax ParseBodyItem()
    {
        if (TryMatch(TokenKind.LetKeyword))
        {
            var name = Expect(TokenKind.Identifier, "Expected a field name after 'let'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the field name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the let override.");
            return new LetSyntax(name, value);
        }

        var typeName = ParseTypeName();
        var nameToken = Expect(TokenKind.Identifier, "Expected a field name.");
        ExpressionSyntax? initializer = null;
        if (TryMatch(TokenKind.Equal))
        {
            initializer = ParseExpression();
        }

        Expect(TokenKind.Semicolon, "Expected ';' after the field declaration.");
        return new FieldSyntax(typeName, nameToken.Text, initializer);
    }

    private string ParseTypeName()
    {
        var name = Expect(TokenKind.Identifier, "Expected a type name.").Text;
        if (!TryMatch(TokenKind.LessThan))
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
                case TokenKind.LessThan:
                    depth++;
                    break;
                case TokenKind.GreaterThan:
                    depth--;
                    break;
                case TokenKind.EndOfFile:
                    throw Error("Unexpected end of file while parsing a type argument list.");
            }

            parts.Add(token.Kind == TokenKind.String ? "\"" + token.Text + "\"" : token.Text);
        }

        return string.Concat(parts);
    }

    private ExpressionSyntax ParseExpression()
    {
        var left = ParsePrimaryExpression();
        while (TryMatch(TokenKind.Hash))
        {
            var right = ParsePrimaryExpression();
            left = new ConcatSyntax(left, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        if (TryMatch(TokenKind.Integer, out var integerToken))
        {
            return new IntegerSyntax(int.Parse(integerToken.Text, CultureInfo.InvariantCulture));
        }

        if (TryMatch(TokenKind.String, out var stringToken))
        {
            return new StringSyntax(stringToken.Text);
        }

        if (TryMatch(TokenKind.CodeBlock, out var codeBlockToken))
        {
            return new StringSyntax(codeBlockToken.Text);
        }

        if (TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            if (Is(TokenKind.LessThan))
            {
                var arguments = ParseOptionalArgumentList();
                Expect(TokenKind.Dot, "Expected '.' after the template argument list.");
                var fieldName = Expect(TokenKind.Identifier, "Expected a field name.").Text;
                return new ClassInstantiationSyntax(identifierToken.Text, arguments, fieldName);
            }

            return new IdentifierSyntax(identifierToken.Text);
        }

        if (TryMatch(TokenKind.BangKeyword, out var bangToken))
        {
            return ParseBangExpression(bangToken.Text);
        }

        if (TryMatch(TokenKind.LBracket))
        {
            var items = new List<ExpressionSyntax>();
            if (!TryMatch(TokenKind.RBracket))
            {
                do
                {
                    items.Add(ParseExpression());
                }
                while (TryMatch(TokenKind.Comma));

                Expect(TokenKind.RBracket, "Expected ']' to close the list literal.");
            }

            return new ListSyntax(items);
        }

        if (TryMatch(TokenKind.LParen))
        {
            var operatorName = Expect(TokenKind.Identifier, "Expected a dag operator name.").Text;
            var arguments = new List<DagArgumentSyntax>();
            while (!TryMatch(TokenKind.RParen))
            {
                var value = ParseExpression();
                string? name = null;
                if (TryMatch(TokenKind.Colon))
                {
                    TryMatch(TokenKind.Dollar);
                    name = Expect(TokenKind.Identifier, "Expected a dag argument name.").Text;
                }

                arguments.Add(new DagArgumentSyntax(value, name));
                if (TryMatch(TokenKind.RParen))
                {
                    break;
                }

                Expect(TokenKind.Comma, "Expected ',' or ')' in the dag argument list.");
            }

            return new DagSyntax(operatorName, arguments);
        }

        throw Error("Expected an expression.");
    }

    private ExpressionSyntax ParseBangExpression(string operatorName)
    {
        if (operatorName == "foldl")
        {
            Expect(TokenKind.LParen, "Expected '(' after '!foldl'.");
            var init = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var list = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var accVar = Expect(TokenKind.Identifier, "Expected an accumulator variable name in '!foldl'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var curVar = Expect(TokenKind.Identifier, "Expected a current-element variable name in '!foldl'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var body = ParseExpression();
            Expect(TokenKind.RParen, "Expected ')' to close '!foldl'.");
            return new FoldlSyntax(init, list, accVar, curVar, body);
        }

        Expect(TokenKind.LParen, $"Expected '(' after '!{operatorName}'.");
        var args = new List<ExpressionSyntax>();
        if (!TryMatch(TokenKind.RParen))
        {
            do
            {
                args.Add(ParseExpression());
            }
            while (TryMatch(TokenKind.Comma));

            Expect(TokenKind.RParen, $"Expected ')' to close '!{operatorName}'.");
        }

        return new BangCallSyntax(operatorName, args);
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private Token Current => position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];

    private bool TryMatch(TokenKind kind)
    {
        if (!Is(kind))
        {
            return false;
        }

        position++;
        return true;
    }

    private bool TryMatch(TokenKind kind, out Token token)
    {
        if (!Is(kind))
        {
            token = default;
            return false;
        }

        token = tokens[position++];
        return true;
    }

    private Token Expect(TokenKind kind, string message)
    {
        if (!Is(kind))
        {
            throw Error(message);
        }

        return tokens[position++];
    }

    private Token Consume()
    {
        return position < tokens.Count ? tokens[position++] : tokens[tokens.Count - 1];
    }

    private ParseException Error(string message)
    {
        var token = Current;
        return new ParseException(new Diagnostic(message, token.Line, token.Column));
    }
}
