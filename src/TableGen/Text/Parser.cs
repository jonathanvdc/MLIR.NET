namespace TableGen.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TableGen.Syntax;

internal sealed class Parser
{
    private readonly IReadOnlyList<Token> tokens;
    private readonly string? sourceFilePath;
    private int position;

    private Parser(string source, string? sourceFilePath)
    {
        tokens = Lexer.Lex(source, sourceFilePath);
        this.sourceFilePath = sourceFilePath;
    }

    public static DocumentSyntax ParseDocument(string source, string? sourceFilePath = null)
    {
        return new Parser(source, sourceFilePath).ParseDocumentCore();
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
        if (TryMatch(TokenKind.IncludeKeyword))
        {
            var path = Expect(TokenKind.String, "Expected a string literal after 'include'.");
            return new IncludeDirectiveSyntax(path.Text);
        }

        if (TryMatch(TokenKind.ClassKeyword))
        {
            return ParseClass();
        }

        if (TryMatch(TokenKind.DefKeyword))
        {
            return ParseDef();
        }

        if (TryMatch(TokenKind.DefVarKeyword))
        {
            var name = ExpectName("Expected a name after 'defvar'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the defvar name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the defvar declaration.");
            return new DefVarSyntax(name, value);
        }

        throw Error("Expected 'class', 'def', or 'defvar'.");
    }

    private ClassSyntax ParseClass()
    {
        var name = ExpectName("Expected a class name.").Text;
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
        var name = ExpectName("Expected a definition name.").Text;
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
            var name = ExpectName("Expected a template parameter name.").Text;
            ExpressionSyntax? defaultValue = null;
            if (TryMatch(TokenKind.Equal))
            {
                defaultValue = ParseExpression();
            }

            parameters.Add(new TemplateParameterSyntax(typeName, name, defaultValue));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.GreaterThan));

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
            var name = ExpectName("Expected a base-class name.").Text;
            bases.Add(new BaseSyntax(name, ParseOptionalArgumentList()));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.GreaterThan));

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
            var name = ExpectName("Expected a field name after 'let'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the field name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the let override.");
            return new LetSyntax(name, value);
        }

        if (TryMatch(TokenKind.DefVarKeyword))
        {
            var name = ExpectName("Expected a name after 'defvar'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the defvar name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the defvar declaration.");
            return new LocalDefVarSyntax(name, value);
        }

        if (TryMatch(TokenKind.AssertKeyword))
        {
            var condition = ParseExpression();
            ExpressionSyntax? message = null;
            if (TryMatch(TokenKind.Comma))
            {
                message = ParseExpression();
            }

            Expect(TokenKind.Semicolon, "Expected ';' after the assert statement.");
            return new AssertSyntax(condition, message);
        }

        var typeName = ParseTypeName();
        var nameToken = ExpectName("Expected a field name.");
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
        var name = ExpectName("Expected a type name.").Text;
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
        if (TryMatch(TokenKind.QuestionMark))
        {
            return ApplyPostfixAccess(new UnsetSyntax());
        }

        if (TryMatch(TokenKind.Integer, out var integerToken))
        {
            return ApplyPostfixAccess(new IntegerSyntax(int.Parse(integerToken.Text, CultureInfo.InvariantCulture)));
        }

        if (TryMatch(TokenKind.String, out var stringToken))
        {
            return ApplyPostfixAccess(ParseAdjacentStringLiterals(new StringSyntax(stringToken.Text)));
        }

        if (TryMatch(TokenKind.CodeBlock, out var codeBlockToken))
        {
            return ApplyPostfixAccess(ParseAdjacentStringLiterals(new StringSyntax(codeBlockToken.Text)));
        }

        if (TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            if (Is(TokenKind.LessThan))
            {
                var arguments = ParseOptionalArgumentList();
                return ApplyPostfixAccess(new AnonymousClassInstantiationSyntax(identifierToken.Text, arguments));
            }

            return ApplyPostfixAccess(new IdentifierSyntax(identifierToken.Text));
        }

        if (TryMatchName(out var nameToken))
        {
            if (Is(TokenKind.LessThan))
            {
                var arguments = ParseOptionalArgumentList();
                return ApplyPostfixAccess(new AnonymousClassInstantiationSyntax(nameToken.Text, arguments));
            }

            return ApplyPostfixAccess(new IdentifierSyntax(nameToken.Text));
        }

        if (TryMatch(TokenKind.BangKeyword, out var bangToken))
        {
            return ApplyPostfixAccess(ParseBangExpression(bangToken.Text));
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
                while (TryMatch(TokenKind.Comma) && !Is(TokenKind.RBracket));

                Expect(TokenKind.RBracket, "Expected ']' to close the list literal.");
            }

            return ApplyPostfixAccess(new ListSyntax(items));
        }

        if (TryMatch(TokenKind.LParen))
        {
            var operatorName = ExpectName("Expected a dag operator name.").Text;
            var arguments = new List<DagArgumentSyntax>();
            while (!TryMatch(TokenKind.RParen))
            {
                var value = ParseExpression();
                string? name = null;
                if (TryMatch(TokenKind.Colon))
                {
                    TryMatch(TokenKind.Dollar);
                    name = ExpectName("Expected a dag argument name.").Text;
                }

                arguments.Add(new DagArgumentSyntax(value, name));
                if (TryMatch(TokenKind.RParen))
                {
                    break;
                }

                Expect(TokenKind.Comma, "Expected ',' or ')' in the dag argument list.");
                if (TryMatch(TokenKind.RParen))
                {
                    break;
                }
            }

            return ApplyPostfixAccess(new DagSyntax(operatorName, arguments));
        }

        throw Error("Expected an expression.");
    }

    private ExpressionSyntax ParseAdjacentStringLiterals(ExpressionSyntax left)
    {
        while (true)
        {
            if (TryMatch(TokenKind.String, out var stringToken))
            {
                left = new ConcatSyntax(left, new StringSyntax(stringToken.Text));
                continue;
            }

            if (TryMatch(TokenKind.CodeBlock, out var codeBlockToken))
            {
                left = new ConcatSyntax(left, new StringSyntax(codeBlockToken.Text));
                continue;
            }

            return left;
        }
    }

    private ExpressionSyntax ApplyPostfixAccess(ExpressionSyntax expr)
    {
        while (true)
        {
            if (TryMatch(TokenKind.Dot))
            {
                var field = Expect(TokenKind.Identifier, "Expected a field name after '.'.");
                expr = new FieldAccessSyntax(expr, field.Text);
                continue;
            }

            if (TryMatch(TokenKind.LBracket))
            {
                var index = ParseExpression();
                Expect(TokenKind.RBracket, "Expected ']' to close the subscript expression.");
                expr = new SubscriptSyntax(expr, index);
                continue;
            }

            return expr;
        }
    }

    private ExpressionSyntax ParseBangExpression(string operatorName)
    {
        string? typeArgument = null;
        if (TryMatch(TokenKind.LessThan))
        {
            typeArgument = ParseTypeArgument();
        }

        if (operatorName == "foldl")
        {
            Expect(TokenKind.LParen, "Expected '(' after '!foldl'.");
            var init = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var list = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var accVar = ExpectName("Expected an accumulator variable name in '!foldl'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var curVar = ExpectName("Expected a current-element variable name in '!foldl'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            var body = ParseExpression();
            Expect(TokenKind.RParen, "Expected ')' to close '!foldl'.");
            return new FoldlSyntax(init, list, accVar, curVar, body);
        }

        if (operatorName == "foreach")
        {
            Expect(TokenKind.LParen, "Expected '(' after '!foreach'.");
            var varName = ExpectName("Expected variable name in '!foreach'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!foreach'.");
            var list = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!foreach'.");
            var body = ParseExpression();
            Expect(TokenKind.RParen, "Expected ')' to close '!foreach'.");
            return new ForeachSyntax(varName, list, body);
        }

        if (operatorName == "filter")
        {
            Expect(TokenKind.LParen, "Expected '(' after '!filter'.");
            var varName = ExpectName("Expected variable name in '!filter'.").Text;
            Expect(TokenKind.Comma, "Expected ',' in '!filter'.");
            var list = ParseExpression();
            Expect(TokenKind.Comma, "Expected ',' in '!filter'.");
            var predicate = ParseExpression();
            Expect(TokenKind.RParen, "Expected ')' to close '!filter'.");
            return new BangCallSyntax("filter", [new IdentifierSyntax(varName), list, predicate], typeArgument);
        }

        if (operatorName == "cond")
        {
            Expect(TokenKind.LParen, "Expected '(' after '!cond'.");
            var condArgs = new List<ExpressionSyntax>();
            if (!TryMatch(TokenKind.RParen))
            {
                while (true)
                {
                    condArgs.Add(ParseExpression());
                    Expect(TokenKind.Colon, "Expected ':' between a '!cond' condition and value.");
                    condArgs.Add(ParseExpression());
                    if (!TryMatch(TokenKind.Comma))
                    {
                        break;
                    }

                    if (Is(TokenKind.RParen))
                    {
                        break;
                    }
                }

                Expect(TokenKind.RParen, "Expected ')' to close '!cond'.");
            }

            return new BangCallSyntax("cond", condArgs);
        }

        Expect(TokenKind.LParen, $"Expected '(' after '!{operatorName}'.");
        var args = new List<ExpressionSyntax>();
        if (!TryMatch(TokenKind.RParen))
        {
            do
            {
                args.Add(ParseExpression());
            }
            while (TryMatch(TokenKind.Comma) && !Is(TokenKind.RParen));

            Expect(TokenKind.RParen, $"Expected ')' to close '!{operatorName}'.");
        }

        return new BangCallSyntax(operatorName, args, typeArgument);
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private string ParseTypeArgument()
    {
        var parts = new List<string>();
        var depth = 1;
        while (depth > 0)
        {
            var token = Consume();
            switch (token.Kind)
            {
                case TokenKind.LessThan:
                    depth++;
                    parts.Add("<");
                    break;
                case TokenKind.GreaterThan:
                    depth--;
                    if (depth > 0)
                    {
                        parts.Add(">");
                    }

                    break;
                case TokenKind.EndOfFile:
                    throw Error("Unexpected end of file while parsing a bang operator type argument.");
                case TokenKind.String:
                    parts.Add("\"" + token.Text + "\"");
                    break;
                default:
                    parts.Add(token.Text);
                    break;
            }
        }

        return string.Concat(parts);
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

    private Token ExpectName(string message)
    {
        if (TryMatchName(out var token))
        {
            return token;
        }

        throw Error(message);
    }

    private bool TryMatchName(out Token token)
    {
        if (IsNameTokenKind(Current.Kind))
        {
            token = tokens[position++];
            return true;
        }

        token = default;
        return false;
    }

    private static bool IsNameTokenKind(TokenKind kind)
    {
        return kind is TokenKind.Identifier
            or TokenKind.ClassKeyword
            or TokenKind.DefKeyword
            or TokenKind.LetKeyword
            or TokenKind.InKeyword
            or TokenKind.IncludeKeyword
            or TokenKind.AssertKeyword
            or TokenKind.DefVarKeyword;
    }

    private Token Consume()
    {
        return position < tokens.Count ? tokens[position++] : tokens[tokens.Count - 1];
    }

    private ParseException Error(string message)
    {
        var token = Current;
        return new ParseException(new Diagnostic(message, token.Line, token.Column, sourceFilePath));
    }
}
