namespace TableGen.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Text;
using TableGen.Syntax;

/// <summary>
/// Parses the token stream emitted by <see cref="Lexer"/> into TableGen syntax nodes.
/// </summary>
internal sealed class Parser
{
    private readonly IReadOnlyList<Token> tokens;
    private int position;

    private Parser(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
    }

    /// <summary>
    /// Parses a complete TableGen document.
    /// </summary>
    public static ParseResult<DocumentSyntax> ParseDocument(SourceDocument sourceDocument)
    {
        var lexResult = Lexer.Lex(sourceDocument);
        if (!lexResult.IsSuccess)
        {
            return ParseResult<DocumentSyntax>.Failure(lexResult.Diagnostic!);
        }

        return new Parser(lexResult.Value).ParseDocumentCore();
    }

    /// <summary>
    /// Parses a complete TableGen document.
    /// </summary>
    public static ParseResult<DocumentSyntax> ParseDocument(string source)
    {
        return ParseDocument(new StringDocument(string.Empty, source ?? string.Empty));
    }

    private ParseResult<DocumentSyntax> ParseDocumentCore()
    {
        var declarations = new List<TopLevelSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            var items = ParseTopLevelItems([]);
            if (!items.IsSuccess)
            {
                return ParseResult<DocumentSyntax>.Failure(items.Diagnostic!);
            }

            declarations.AddRange(items.Value);
        }

        return ParseResult<DocumentSyntax>.Success(new DocumentSyntax(declarations));
    }

    private ParseResult<IReadOnlyList<TopLevelSyntax>> ParseTopLevelItems(IReadOnlyList<LetSyntax> topLevelLets)
    {
        if (TryMatch(TokenKind.IncludeKeyword))
        {
            var path = Expect(TokenKind.String, "Expected a string literal after 'include'.");
            return path.Map<IReadOnlyList<TopLevelSyntax>>(static token =>
                [new IncludeDirectiveSyntax(token.Text, token.Location)]);
        }

        if (TryMatch(TokenKind.LetKeyword, out var topLevelLet))
        {
            var name = ExpectName("Expected a field name after 'let'.");
            if (!name.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(name);

            var equal = Expect(TokenKind.Equal, "Expected '=' after the field name.");
            if (!equal.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(equal);

            var value = ParseExpression();
            if (!value.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(value);

            var inKeyword = Expect(TokenKind.InKeyword, "Expected 'in' after the top-level let binding.");
            if (!inKeyword.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(inKeyword);

            var nestedLets = topLevelLets.Concat([new LetSyntax(name.Value.Text, value.Value, topLevelLet.Location)]).ToArray();
            var declarations = new List<TopLevelSyntax>();
            if (TryMatch(TokenKind.LBrace))
            {
                while (!TryMatch(TokenKind.RBrace))
                {
                    var nested = ParseTopLevelItems(nestedLets);
                    if (!nested.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(nested);
                    declarations.AddRange(nested.Value);
                }
            }
            else
            {
                var nested = ParseTopLevelItems(nestedLets);
                if (!nested.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(nested);
                declarations.AddRange(nested.Value);
            }

            return ParseResult<IReadOnlyList<TopLevelSyntax>>.Success(declarations);
        }

        if (TryMatch(TokenKind.ClassKeyword, out var classKeyword))
        {
            return ParseClass(topLevelLets, classKeyword.Location).Map<IReadOnlyList<TopLevelSyntax>>(static item => [item]);
        }

        if (TryMatch(TokenKind.DefKeyword, out var defKeyword))
        {
            return ParseDef(topLevelLets, defKeyword.Location).Map<IReadOnlyList<TopLevelSyntax>>(static item => [item]);
        }

        if (TryMatch(TokenKind.DefVarKeyword, out var defVarKeyword))
        {
            var name = ExpectName("Expected a name after 'defvar'.");
            if (!name.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(name);

            var equal = Expect(TokenKind.Equal, "Expected '=' after the defvar name.");
            if (!equal.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(equal);

            var value = ParseExpression();
            if (!value.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(value);

            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the defvar declaration.");
            if (!semicolon.IsSuccess) return Failure<IReadOnlyList<TopLevelSyntax>>(semicolon);

            return ParseResult<IReadOnlyList<TopLevelSyntax>>.Success([new DefVarSyntax(name.Value.Text, value.Value, defVarKeyword.Location)]);
        }

        if (Is(TokenKind.Identifier) && Current.Text == "extends")
        {
            return ParseExtends(topLevelLets).Map<IReadOnlyList<TopLevelSyntax>>(static item => [item]);
        }

        return Error<IReadOnlyList<TopLevelSyntax>>("Expected 'class', 'def', 'defvar', 'let', or 'extends'.");
    }

    private ParseResult<ClassSyntax> ParseClass(IReadOnlyList<LetSyntax> topLevelLets, SourceLocation location)
    {
        var name = ExpectName("Expected a class name.");
        if (!name.IsSuccess) return Failure<ClassSyntax>(name);

        var templateParameters = ParseOptionalTemplateParameters();
        if (!templateParameters.IsSuccess) return Failure<ClassSyntax>(templateParameters);

        var bases = ParseOptionalBases();
        if (!bases.IsSuccess) return Failure<ClassSyntax>(bases);

        var body = ParseOptionalBody();
        if (!body.IsSuccess) return Failure<ClassSyntax>(body);

        if (!body.Value.HadBraces || Is(TokenKind.Semicolon))
        {
            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the class declaration.");
            if (!semicolon.IsSuccess) return Failure<ClassSyntax>(semicolon);
        }

        return ParseResult<ClassSyntax>.Success(
            new ClassSyntax(name.Value.Text, templateParameters.Value, bases.Value, topLevelLets, body.Value.Items, location));
    }

    private ParseResult<DefSyntax> ParseDef(IReadOnlyList<LetSyntax> topLevelLets, SourceLocation location)
    {
        var name = ExpectName("Expected a definition name.");
        if (!name.IsSuccess) return Failure<DefSyntax>(name);

        var bases = ParseOptionalBases();
        if (!bases.IsSuccess) return Failure<DefSyntax>(bases);

        var body = ParseOptionalBody();
        if (!body.IsSuccess) return Failure<DefSyntax>(body);

        if (!body.Value.HadBraces || Is(TokenKind.Semicolon))
        {
            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the definition.");
            if (!semicolon.IsSuccess) return Failure<DefSyntax>(semicolon);
        }

        return ParseResult<DefSyntax>.Success(new DefSyntax(name.Value.Text, bases.Value, topLevelLets, body.Value.Items, location));
    }

    private ParseResult<ExtendsSyntax> ParseExtends(IReadOnlyList<LetSyntax> topLevelLets)
    {
        var extends = Expect(TokenKind.Identifier, "Expected 'extends'.");
        if (!extends.IsSuccess) return Failure<ExtendsSyntax>(extends);

        var targetName = ExpectName("Expected a target record name after 'extends'.");
        if (!targetName.IsSuccess) return Failure<ExtendsSyntax>(targetName);

        var colon = Expect(TokenKind.Colon, "Expected ':' after the target record name.");
        if (!colon.IsSuccess) return Failure<ExtendsSyntax>(colon);

        var bases = ParseExtendsBases();
        if (!bases.IsSuccess) return Failure<ExtendsSyntax>(bases);

        var lets = ParseOptionalExtendsBody();
        if (!lets.IsSuccess) return Failure<ExtendsSyntax>(lets);

        if (!lets.Value.HadBraces || Is(TokenKind.Semicolon))
        {
            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the extends declaration.");
            if (!semicolon.IsSuccess) return Failure<ExtendsSyntax>(semicolon);
        }

        return ParseResult<ExtendsSyntax>.Success(
            new ExtendsSyntax(targetName.Value.Text, bases.Value, topLevelLets, lets.Value.Items, extends.Value.Location));
    }

    private ParseResult<IReadOnlyList<TemplateParameterSyntax>> ParseOptionalTemplateParameters()
    {
        var parameters = new List<TemplateParameterSyntax>();
        if (!TryMatch(TokenKind.LessThan))
        {
            return ParseResult<IReadOnlyList<TemplateParameterSyntax>>.Success(parameters);
        }

        if (TryMatch(TokenKind.GreaterThan))
        {
            return ParseResult<IReadOnlyList<TemplateParameterSyntax>>.Success(parameters);
        }

        do
        {
            var typeName = ParseTypeName();
            if (!typeName.IsSuccess) return Failure<IReadOnlyList<TemplateParameterSyntax>>(typeName);

            var name = ExpectName("Expected a template parameter name.");
            if (!name.IsSuccess) return Failure<IReadOnlyList<TemplateParameterSyntax>>(name);

            ExpressionSyntax? defaultValue = null;
            if (TryMatch(TokenKind.Equal))
            {
                var parsedDefault = ParseExpression();
                if (!parsedDefault.IsSuccess) return Failure<IReadOnlyList<TemplateParameterSyntax>>(parsedDefault);
                defaultValue = parsedDefault.Value;
            }

            parameters.Add(new TemplateParameterSyntax(typeName.Value, name.Value.Text, defaultValue, name.Value.Location));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.GreaterThan));

        var close = Expect(TokenKind.GreaterThan, "Expected '>' to close the template parameter list.");
        return close.IsSuccess
            ? ParseResult<IReadOnlyList<TemplateParameterSyntax>>.Success(parameters)
            : Failure<IReadOnlyList<TemplateParameterSyntax>>(close);
    }

    private ParseResult<IReadOnlyList<BaseSyntax>> ParseOptionalBases()
    {
        var bases = new List<BaseSyntax>();
        if (!TryMatch(TokenKind.Colon))
        {
            return ParseResult<IReadOnlyList<BaseSyntax>>.Success(bases);
        }

        do
        {
            var name = ExpectName("Expected a base-class name.");
            if (!name.IsSuccess) return Failure<IReadOnlyList<BaseSyntax>>(name);

            var args = ParseOptionalArgumentList();
            if (!args.IsSuccess) return Failure<IReadOnlyList<BaseSyntax>>(args);

            bases.Add(new BaseSyntax(name.Value.Text, args.Value, name.Value.Location));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.GreaterThan));

        return ParseResult<IReadOnlyList<BaseSyntax>>.Success(bases);
    }

    private ParseResult<IReadOnlyList<ExpressionSyntax>> ParseOptionalArgumentList()
    {
        var arguments = new List<ExpressionSyntax>();
        if (!TryMatch(TokenKind.LessThan))
        {
            return ParseResult<IReadOnlyList<ExpressionSyntax>>.Success(arguments);
        }

        if (TryMatch(TokenKind.GreaterThan))
        {
            return ParseResult<IReadOnlyList<ExpressionSyntax>>.Success(arguments);
        }

        do
        {
            var arg = ParseExpression();
            if (!arg.IsSuccess) return Failure<IReadOnlyList<ExpressionSyntax>>(arg);
            arguments.Add(arg.Value);
        }
        while (TryMatch(TokenKind.Comma));

        var close = Expect(TokenKind.GreaterThan, "Expected '>' to close the argument list.");
        return close.IsSuccess
            ? ParseResult<IReadOnlyList<ExpressionSyntax>>.Success(arguments)
            : Failure<IReadOnlyList<ExpressionSyntax>>(close);
    }

    private ParseResult<BodyResult> ParseOptionalBody()
    {
        var items = new List<BodyItemSyntax>();
        if (!TryMatch(TokenKind.LBrace))
        {
            return ParseResult<BodyResult>.Success(new BodyResult(false, items));
        }

        while (!TryMatch(TokenKind.RBrace))
        {
            var item = ParseBodyItem();
            if (!item.IsSuccess) return Failure<BodyResult>(item);
            items.Add(item.Value);
        }

        return ParseResult<BodyResult>.Success(new BodyResult(true, items));
    }

    private ParseResult<LetBodyResult> ParseOptionalExtendsBody()
    {
        var items = new List<LetSyntax>();
        if (!TryMatch(TokenKind.LBrace))
        {
            return ParseResult<LetBodyResult>.Success(new LetBodyResult(false, items));
        }

        while (!TryMatch(TokenKind.RBrace))
        {
            if (!TryMatch(TokenKind.LetKeyword))
            {
                return Error<LetBodyResult>("Expected 'let' or '}' in an 'extends' body.");
            }

            var name = ExpectName("Expected a field name after 'let'.");
            if (!name.IsSuccess) return Failure<LetBodyResult>(name);

            var equal = Expect(TokenKind.Equal, "Expected '=' after the field name.");
            if (!equal.IsSuccess) return Failure<LetBodyResult>(equal);

            var value = ParseExpression();
            if (!value.IsSuccess) return Failure<LetBodyResult>(value);

            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the let override.");
            if (!semicolon.IsSuccess) return Failure<LetBodyResult>(semicolon);

            items.Add(new LetSyntax(name.Value.Text, value.Value, name.Value.Location));
        }

        return ParseResult<LetBodyResult>.Success(new LetBodyResult(true, items));
    }

    private ParseResult<IReadOnlyList<BaseSyntax>> ParseExtendsBases()
    {
        var bases = new List<BaseSyntax>();
        do
        {
            var name = ExpectName("Expected a schema class name after ':'.");
            if (!name.IsSuccess) return Failure<IReadOnlyList<BaseSyntax>>(name);

            var args = ParseOptionalArgumentList();
            if (!args.IsSuccess) return Failure<IReadOnlyList<BaseSyntax>>(args);

            bases.Add(new BaseSyntax(name.Value.Text, args.Value, name.Value.Location));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.LBrace) && !Is(TokenKind.Semicolon));

        return ParseResult<IReadOnlyList<BaseSyntax>>.Success(bases);
    }

    private ParseResult<BodyItemSyntax> ParseBodyItem()
    {
        if (TryMatch(TokenKind.LetKeyword))
        {
            var name = ExpectName("Expected a field name after 'let'.");
            if (!name.IsSuccess) return Failure<BodyItemSyntax>(name);

            var equal = Expect(TokenKind.Equal, "Expected '=' after the field name.");
            if (!equal.IsSuccess) return Failure<BodyItemSyntax>(equal);

            var value = ParseExpression();
            if (!value.IsSuccess) return Failure<BodyItemSyntax>(value);

            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the let override.");
            if (!semicolon.IsSuccess) return Failure<BodyItemSyntax>(semicolon);

            return ParseResult<BodyItemSyntax>.Success(new LetSyntax(name.Value.Text, value.Value, name.Value.Location));
        }

        if (TryMatch(TokenKind.DefVarKeyword))
        {
            var name = ExpectName("Expected a name after 'defvar'.");
            if (!name.IsSuccess) return Failure<BodyItemSyntax>(name);

            var equal = Expect(TokenKind.Equal, "Expected '=' after the defvar name.");
            if (!equal.IsSuccess) return Failure<BodyItemSyntax>(equal);

            var value = ParseExpression();
            if (!value.IsSuccess) return Failure<BodyItemSyntax>(value);

            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the defvar declaration.");
            if (!semicolon.IsSuccess) return Failure<BodyItemSyntax>(semicolon);

            return ParseResult<BodyItemSyntax>.Success(new LocalDefVarSyntax(name.Value.Text, value.Value, name.Value.Location));
        }

        if (TryMatch(TokenKind.AssertKeyword))
        {
            var condition = ParseExpression();
            if (!condition.IsSuccess) return Failure<BodyItemSyntax>(condition);

            ExpressionSyntax? message = null;
            if (TryMatch(TokenKind.Comma))
            {
                var parsedMessage = ParseExpression();
                if (!parsedMessage.IsSuccess) return Failure<BodyItemSyntax>(parsedMessage);
                message = parsedMessage.Value;
            }

            var semicolon = Expect(TokenKind.Semicolon, "Expected ';' after the assert statement.");
            if (!semicolon.IsSuccess) return Failure<BodyItemSyntax>(semicolon);

            return ParseResult<BodyItemSyntax>.Success(new AssertSyntax(condition.Value, message, condition.Value.Location));
        }

        var typeName = ParseTypeName();
        if (!typeName.IsSuccess) return Failure<BodyItemSyntax>(typeName);

        var nameToken = ExpectName("Expected a field name.");
        if (!nameToken.IsSuccess) return Failure<BodyItemSyntax>(nameToken);

        ExpressionSyntax? initializer = null;
        if (TryMatch(TokenKind.Equal))
        {
            var parsedInitializer = ParseExpression();
            if (!parsedInitializer.IsSuccess) return Failure<BodyItemSyntax>(parsedInitializer);
            initializer = parsedInitializer.Value;
        }

        var fieldSemicolon = Expect(TokenKind.Semicolon, "Expected ';' after the field declaration.");
        if (!fieldSemicolon.IsSuccess) return Failure<BodyItemSyntax>(fieldSemicolon);

        return ParseResult<BodyItemSyntax>.Success(new FieldSyntax(typeName.Value, nameToken.Value.Text, initializer, nameToken.Value.Location));
    }

    private ParseResult<string> ParseTypeName()
    {
        var name = ExpectName("Expected a type name.");
        if (!name.IsSuccess) return Failure<string>(name);

        if (!TryMatch(TokenKind.LessThan))
        {
            return ParseResult<string>.Success(name.Value.Text);
        }

        var parts = new List<string> { name.Value.Text, "<" };
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
                    return Error<string>("Unexpected end of file while parsing a type argument list.");
            }

            parts.Add(token.Kind == TokenKind.String ? "\"" + token.Text + "\"" : token.Text);
        }

        return ParseResult<string>.Success(string.Concat(parts));
    }

    private ParseResult<ExpressionSyntax> ParseExpression()
    {
        var left = ParsePrimaryExpression();
        if (!left.IsSuccess) return left;

        var expr = left.Value;
        while (TryMatch(TokenKind.Hash))
        {
            var right = ParsePrimaryExpression();
            if (!right.IsSuccess) return right;
            expr = new ConcatSyntax(expr, right.Value, SourceLocation.Merge(expr.Location, right.Value.Location));
        }

        return ParseResult<ExpressionSyntax>.Success(expr);
    }

    private ParseResult<ExpressionSyntax> ParsePrimaryExpression()
    {
        if (TryMatch(TokenKind.QuestionMark, out var questionToken))
        {
            return ApplyPostfixAccess(new UnsetSyntax(questionToken.Location));
        }

        if (TryMatch(TokenKind.Integer, out var integerToken))
        {
            return ApplyPostfixAccess(new IntegerSyntax(int.Parse(integerToken.Text, CultureInfo.InvariantCulture), integerToken.Location));
        }

        if (TryMatch(TokenKind.String, out var stringToken))
        {
            return ApplyPostfixAccess(ParseAdjacentStringLiterals(new StringSyntax(stringToken.Text, stringToken.Location)));
        }

        if (TryMatch(TokenKind.CodeBlock, out var codeBlockToken))
        {
            return ApplyPostfixAccess(ParseAdjacentStringLiterals(new StringSyntax(codeBlockToken.Text, codeBlockToken.Location)));
        }

        if (TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            return ParseIdentifierLikePrimary(identifierToken);
        }

        if (TryMatchName(out var nameToken))
        {
            return ParseIdentifierLikePrimary(nameToken);
        }

        if (TryMatch(TokenKind.BangKeyword, out var bangToken))
        {
            var bang = ParseBangExpression(bangToken.Text, bangToken.Location);
            return bang.IsSuccess ? ApplyPostfixAccess(bang.Value) : bang;
        }

        if (TryMatch(TokenKind.LBracket, out var listOpen))
        {
            var items = new List<ExpressionSyntax>();
            if (!TryMatch(TokenKind.RBracket))
            {
                do
                {
                    var item = ParseExpression();
                    if (!item.IsSuccess) return item;
                    items.Add(item.Value);
                }
                while (TryMatch(TokenKind.Comma) && !Is(TokenKind.RBracket));

                var close = Expect(TokenKind.RBracket, "Expected ']' to close the list literal.");
                if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
            }

            return ApplyPostfixAccess(new ListSyntax(items, listOpen.Location));
        }

        if (TryMatch(TokenKind.LParen, out var dagOpen))
        {
            var operatorName = ExpectName("Expected a dag operator name.");
            if (!operatorName.IsSuccess) return Failure<ExpressionSyntax>(operatorName);

            var arguments = new List<DagArgumentSyntax>();
            while (!TryMatch(TokenKind.RParen))
            {
                var value = ParseExpression();
                if (!value.IsSuccess) return value;

                string? name = null;
                if (TryMatch(TokenKind.Colon))
                {
                    TryMatch(TokenKind.Dollar);
                    var argName = ExpectName("Expected a dag argument name.");
                    if (!argName.IsSuccess) return Failure<ExpressionSyntax>(argName);
                    name = argName.Value.Text;
                }

                arguments.Add(new DagArgumentSyntax(value.Value, name));
                if (TryMatch(TokenKind.RParen))
                {
                    break;
                }

                var comma = Expect(TokenKind.Comma, "Expected ',' or ')' in the dag argument list.");
                if (!comma.IsSuccess) return Failure<ExpressionSyntax>(comma);
                if (TryMatch(TokenKind.RParen))
                {
                    break;
                }
            }

            return ApplyPostfixAccess(new DagSyntax(operatorName.Value.Text, arguments, dagOpen.Location));
        }

        return Error<ExpressionSyntax>("Expected an expression.");
    }

    private ParseResult<ExpressionSyntax> ParseIdentifierLikePrimary(Token token)
    {
        if (Is(TokenKind.LessThan))
        {
            var arguments = ParseOptionalArgumentList();
            if (!arguments.IsSuccess) return Failure<ExpressionSyntax>(arguments);

            var bodyLets = ParseOptionalExtendsBody();
            if (!bodyLets.IsSuccess) return Failure<ExpressionSyntax>(bodyLets);

            return ApplyPostfixAccess(new AnonymousClassInstantiationSyntax(token.Text, arguments.Value, bodyLets.Value.Items, token.Location));
        }

        return ApplyPostfixAccess(new IdentifierSyntax(token.Text, token.Location));
    }

    private ExpressionSyntax ParseAdjacentStringLiterals(ExpressionSyntax left)
    {
        while (true)
        {
            if (TryMatch(TokenKind.String, out var stringToken))
            {
                var right = new StringSyntax(stringToken.Text, stringToken.Location);
                left = new ConcatSyntax(left, right, SourceLocation.Merge(left.Location, right.Location));
                continue;
            }

            if (TryMatch(TokenKind.CodeBlock, out var codeBlockToken))
            {
                var right = new StringSyntax(codeBlockToken.Text, codeBlockToken.Location);
                left = new ConcatSyntax(left, right, SourceLocation.Merge(left.Location, right.Location));
                continue;
            }

            return left;
        }
    }

    private ParseResult<ExpressionSyntax> ApplyPostfixAccess(ExpressionSyntax expr)
    {
        while (true)
        {
            if (TryMatch(TokenKind.Dot))
            {
                var field = Expect(TokenKind.Identifier, "Expected a field name after '.'.");
                if (!field.IsSuccess) return Failure<ExpressionSyntax>(field);
                expr = new FieldAccessSyntax(expr, field.Value.Text, SourceLocation.Merge(expr.Location, field.Value.Location));
                continue;
            }

            if (TryMatch(TokenKind.LBracket))
            {
                var index = ParseExpression();
                if (!index.IsSuccess) return index;

                var close = Expect(TokenKind.RBracket, "Expected ']' to close the subscript expression.");
                if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);

                expr = new SubscriptSyntax(expr, index.Value, SourceLocation.Merge(expr.Location, close.Value.Location));
                continue;
            }

            return ParseResult<ExpressionSyntax>.Success(expr);
        }
    }

    private ParseResult<ExpressionSyntax> ParseBangExpression(string operatorName, SourceLocation location)
    {
        string? typeArgument = null;
        if (TryMatch(TokenKind.LessThan))
        {
            var parsedTypeArgument = ParseTypeArgument();
            if (!parsedTypeArgument.IsSuccess) return Failure<ExpressionSyntax>(parsedTypeArgument);
            typeArgument = parsedTypeArgument.Value;
        }

        if (operatorName == "foldl")
        {
            var open = Expect(TokenKind.LParen, "Expected '(' after '!foldl'.");
            if (!open.IsSuccess) return Failure<ExpressionSyntax>(open);

            var init = ParseExpression();
            if (!init.IsSuccess) return init;
            var comma1 = Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            if (!comma1.IsSuccess) return Failure<ExpressionSyntax>(comma1);
            var list = ParseExpression();
            if (!list.IsSuccess) return list;
            var comma2 = Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            if (!comma2.IsSuccess) return Failure<ExpressionSyntax>(comma2);
            var accVar = ExpectName("Expected an accumulator variable name in '!foldl'.");
            if (!accVar.IsSuccess) return Failure<ExpressionSyntax>(accVar);
            var comma3 = Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            if (!comma3.IsSuccess) return Failure<ExpressionSyntax>(comma3);
            var curVar = ExpectName("Expected a current-element variable name in '!foldl'.");
            if (!curVar.IsSuccess) return Failure<ExpressionSyntax>(curVar);
            var comma4 = Expect(TokenKind.Comma, "Expected ',' in '!foldl'.");
            if (!comma4.IsSuccess) return Failure<ExpressionSyntax>(comma4);
            var body = ParseExpression();
            if (!body.IsSuccess) return body;
            var close = Expect(TokenKind.RParen, "Expected ')' to close '!foldl'.");
            if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
            return ParseResult<ExpressionSyntax>.Success(new FoldlSyntax(init.Value, list.Value, accVar.Value.Text, curVar.Value.Text, body.Value, location));
        }

        if (operatorName == "foreach")
        {
            var open = Expect(TokenKind.LParen, "Expected '(' after '!foreach'.");
            if (!open.IsSuccess) return Failure<ExpressionSyntax>(open);
            var varName = ExpectName("Expected variable name in '!foreach'.");
            if (!varName.IsSuccess) return Failure<ExpressionSyntax>(varName);
            var comma1 = Expect(TokenKind.Comma, "Expected ',' in '!foreach'.");
            if (!comma1.IsSuccess) return Failure<ExpressionSyntax>(comma1);
            var list = ParseExpression();
            if (!list.IsSuccess) return list;
            var comma2 = Expect(TokenKind.Comma, "Expected ',' in '!foreach'.");
            if (!comma2.IsSuccess) return Failure<ExpressionSyntax>(comma2);
            var body = ParseExpression();
            if (!body.IsSuccess) return body;
            var close = Expect(TokenKind.RParen, "Expected ')' to close '!foreach'.");
            if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
            return ParseResult<ExpressionSyntax>.Success(new ForeachSyntax(varName.Value.Text, list.Value, body.Value, location));
        }

        if (operatorName == "filter")
        {
            var open = Expect(TokenKind.LParen, "Expected '(' after '!filter'.");
            if (!open.IsSuccess) return Failure<ExpressionSyntax>(open);
            var varName = ExpectName("Expected variable name in '!filter'.");
            if (!varName.IsSuccess) return Failure<ExpressionSyntax>(varName);
            var comma1 = Expect(TokenKind.Comma, "Expected ',' in '!filter'.");
            if (!comma1.IsSuccess) return Failure<ExpressionSyntax>(comma1);
            var list = ParseExpression();
            if (!list.IsSuccess) return list;
            var comma2 = Expect(TokenKind.Comma, "Expected ',' in '!filter'.");
            if (!comma2.IsSuccess) return Failure<ExpressionSyntax>(comma2);
            var predicate = ParseExpression();
            if (!predicate.IsSuccess) return predicate;
            var close = Expect(TokenKind.RParen, "Expected ')' to close '!filter'.");
            if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
            return ParseResult<ExpressionSyntax>.Success(new BangCallSyntax("filter", [new IdentifierSyntax(varName.Value.Text, varName.Value.Location), list.Value, predicate.Value], typeArgument, location));
        }

        if (operatorName == "cond")
        {
            var open = Expect(TokenKind.LParen, "Expected '(' after '!cond'.");
            if (!open.IsSuccess) return Failure<ExpressionSyntax>(open);

            var condArgs = new List<ExpressionSyntax>();
            if (!TryMatch(TokenKind.RParen))
            {
                while (true)
                {
                    var condition = ParseExpression();
                    if (!condition.IsSuccess) return condition;
                    condArgs.Add(condition.Value);

                    var colon = Expect(TokenKind.Colon, "Expected ':' between a '!cond' condition and value.");
                    if (!colon.IsSuccess) return Failure<ExpressionSyntax>(colon);

                    var value = ParseExpression();
                    if (!value.IsSuccess) return value;
                    condArgs.Add(value.Value);

                    if (!TryMatch(TokenKind.Comma) || Is(TokenKind.RParen))
                    {
                        break;
                    }
                }

                var close = Expect(TokenKind.RParen, "Expected ')' to close '!cond'.");
                if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
            }

            return ParseResult<ExpressionSyntax>.Success(new BangCallSyntax("cond", condArgs, location: location));
        }

        var genericOpen = Expect(TokenKind.LParen, $"Expected '(' after '!{operatorName}'.");
        if (!genericOpen.IsSuccess) return Failure<ExpressionSyntax>(genericOpen);

        var args = new List<ExpressionSyntax>();
        if (!TryMatch(TokenKind.RParen))
        {
            do
            {
                var arg = ParseExpression();
                if (!arg.IsSuccess) return arg;
                args.Add(arg.Value);
            }
            while (TryMatch(TokenKind.Comma) && !Is(TokenKind.RParen));

            var close = Expect(TokenKind.RParen, $"Expected ')' to close '!{operatorName}'.");
            if (!close.IsSuccess) return Failure<ExpressionSyntax>(close);
        }

        return ParseResult<ExpressionSyntax>.Success(new BangCallSyntax(operatorName, args, typeArgument, location));
    }

    private ParseResult<string> ParseTypeArgument()
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
                    return Error<string>("Unexpected end of file while parsing a bang operator type argument.");
                case TokenKind.String:
                    parts.Add("\"" + token.Text + "\"");
                    break;
                default:
                    parts.Add(token.Text);
                    break;
            }
        }

        return ParseResult<string>.Success(string.Concat(parts));
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

    private ParseResult<Token> Expect(TokenKind kind, string message)
    {
        return Is(kind)
            ? ParseResult<Token>.Success(tokens[position++])
            : Error<Token>(message);
    }

    private ParseResult<Token> ExpectName(string message)
    {
        return TryMatchName(out var token)
            ? ParseResult<Token>.Success(token)
            : Error<Token>(message);
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

    private ParseResult<T> Error<T>(string message)
    {
        return ParseResult<T>.Failure(new Diagnostic(message, Current.Location));
    }

    private static ParseResult<T> Failure<T>(IParseResult result)
    {
        return ParseResult<T>.Failure(result.Diagnostic!);
    }

    private readonly struct BodyResult(bool hadBraces, IReadOnlyList<BodyItemSyntax> items)
    {
        public bool HadBraces { get; } = hadBraces;
        public IReadOnlyList<BodyItemSyntax> Items { get; } = items;
    }

    private readonly struct LetBodyResult(bool hadBraces, IReadOnlyList<LetSyntax> items)
    {
        public bool HadBraces { get; } = hadBraces;
        public IReadOnlyList<LetSyntax> Items { get; } = items;
    }
}
