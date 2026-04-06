namespace TableGen.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Parses the token stream emitted by <see cref="Lexer"/> into TableGen syntax nodes.
/// </summary>
internal sealed class Parser
{
    /// <summary>
    /// Stores the token stream being parsed.
    /// </summary>
    private readonly IReadOnlyList<Token> tokens;

    /// <summary>
    /// Stores the logical source path used when reporting parse errors.
    /// </summary>
    private readonly string? sourceFilePath;

    /// <summary>
    /// Tracks the current token position.
    /// </summary>
    private int position;

    /// <summary>
    /// Initializes a parser for a source string.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="sourceFilePath">An optional logical source path for diagnostics.</param>
    private Parser(string source, string? sourceFilePath)
    {
        tokens = Lexer.Lex(source, sourceFilePath);
        this.sourceFilePath = sourceFilePath;
    }

    /// <summary>
    /// Parses a complete TableGen document.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="sourceFilePath">An optional logical source path for diagnostics.</param>
    /// <returns>The parsed syntax tree.</returns>
    public static DocumentSyntax ParseDocument(string source, string? sourceFilePath = null)
    {
        return new Parser(source, sourceFilePath).ParseDocumentCore();
    }

    /// <summary>
    /// Parses a complete TableGen document from the current token stream.
    /// </summary>
    /// <returns>The parsed syntax tree.</returns>
    private DocumentSyntax ParseDocumentCore()
    {
        var declarations = new List<TopLevelSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            declarations.AddRange(ParseTopLevelItems([]));
        }

        return new DocumentSyntax(declarations);
    }

    /// <summary>
    /// Parses one or more top-level declarations, threading any outer <c>let ... in</c> bindings into nested declarations.
    /// </summary>
    /// <param name="topLevelLets">The top-level lets currently in scope.</param>
    /// <returns>The parsed top-level declarations.</returns>
    private IReadOnlyList<TopLevelSyntax> ParseTopLevelItems(IReadOnlyList<LetSyntax> topLevelLets)
    {
        if (TryMatch(TokenKind.IncludeKeyword))
        {
            var path = Expect(TokenKind.String, "Expected a string literal after 'include'.");
            return [new IncludeDirectiveSyntax(path.Text)];
        }

        if (TryMatch(TokenKind.LetKeyword))
        {
            var name = ExpectName("Expected a field name after 'let'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the field name.");
            var value = ParseExpression();
            Expect(TokenKind.InKeyword, "Expected 'in' after the top-level let binding.");

            var nestedLets = topLevelLets.Concat([new LetSyntax(name, value)]).ToArray();
            var declarations = new List<TopLevelSyntax>();
            if (TryMatch(TokenKind.LBrace))
            {
                // `let ... in { ... }` applies to every nested declaration until the matching brace.
                while (!TryMatch(TokenKind.RBrace))
                {
                    declarations.AddRange(ParseTopLevelItems(nestedLets));
                }
            }
            else
            {
                declarations.AddRange(ParseTopLevelItems(nestedLets));
            }

            return declarations;
        }

        if (TryMatch(TokenKind.ClassKeyword))
        {
            return [ParseClass(topLevelLets)];
        }

        if (TryMatch(TokenKind.DefKeyword))
        {
            return [ParseDef(topLevelLets)];
        }

        if (TryMatch(TokenKind.DefVarKeyword))
        {
            var name = ExpectName("Expected a name after 'defvar'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the defvar name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the defvar declaration.");
            return [new DefVarSyntax(name, value)];
        }

        if (Is(TokenKind.Identifier) && Current.Text == "extends")
        {
            return [ParseExtends(topLevelLets)];
        }

        throw Error("Expected 'class', 'def', 'defvar', 'let', or 'extends'.");
    }

    /// <summary>
    /// Parses a <c>class</c> declaration.
    /// </summary>
    /// <param name="topLevelLets">The top-level lets captured for this declaration.</param>
    /// <returns>The parsed class syntax node.</returns>
    private ClassSyntax ParseClass(IReadOnlyList<LetSyntax> topLevelLets)
    {
        var name = ExpectName("Expected a class name.").Text;
        var templateParameters = ParseOptionalTemplateParameters();
        var bases = ParseOptionalBases();
        var (hadBraces, bodyItems) = ParseOptionalBody();
        if (!hadBraces || Is(TokenKind.Semicolon))
        {
            Expect(TokenKind.Semicolon, "Expected ';' after the class declaration.");
        }

        return new ClassSyntax(name, templateParameters, bases, topLevelLets, bodyItems);
    }

    /// <summary>
    /// Parses a <c>def</c> declaration.
    /// </summary>
    /// <param name="topLevelLets">The top-level lets captured for this declaration.</param>
    /// <returns>The parsed definition syntax node.</returns>
    private DefSyntax ParseDef(IReadOnlyList<LetSyntax> topLevelLets)
    {
        var name = ExpectName("Expected a definition name.").Text;
        var bases = ParseOptionalBases();
        var (hadBraces, bodyItems) = ParseOptionalBody();
        if (!hadBraces || Is(TokenKind.Semicolon))
        {
            Expect(TokenKind.Semicolon, "Expected ';' after the definition.");
        }

        return new DefSyntax(name, bases, topLevelLets, bodyItems);
    }

    /// <summary>
    /// Parses an <c>extends</c> overlay declaration.
    /// </summary>
    /// <param name="topLevelLets">The top-level lets captured for this declaration.</param>
    /// <returns>The parsed extends syntax node.</returns>
    private ExtendsSyntax ParseExtends(IReadOnlyList<LetSyntax> topLevelLets)
    {
        Expect(TokenKind.Identifier, "Expected 'extends'.");
        var targetName = ExpectName("Expected a target record name after 'extends'.").Text;
        Expect(TokenKind.Colon, "Expected ':' after the target record name.");
        var bases = ParseExtendsBases();
        var lets = ParseOptionalExtendsBody();
        if (!lets.hadBraces || Is(TokenKind.Semicolon))
        {
            Expect(TokenKind.Semicolon, "Expected ';' after the extends declaration.");
        }

        return new ExtendsSyntax(targetName, bases, topLevelLets, lets.items);
    }

    /// <summary>
    /// Parses an optional template parameter list such as <c>&lt;string name, int n = 0&gt;</c>.
    /// </summary>
    /// <returns>The parsed template parameters, or an empty list when no parameter list is present.</returns>
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

    /// <summary>
    /// Parses an optional base-class list following a colon.
    /// </summary>
    /// <returns>The parsed base list, or an empty list when no bases are present.</returns>
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

    /// <summary>
    /// Parses an optional angle-bracketed argument list.
    /// </summary>
    /// <returns>The parsed arguments, or an empty list when no argument list is present.</returns>
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

    /// <summary>
    /// Parses an optional braced body block.
    /// </summary>
    /// <returns>A tuple indicating whether braces were present and the parsed body items.</returns>
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

    /// <summary>
    /// Parses the body of an <c>extends</c> declaration, which only accepts <c>let</c> assignments.
    /// </summary>
    /// <returns>A tuple indicating whether braces were present and the parsed let bindings.</returns>
    private (bool hadBraces, IReadOnlyList<LetSyntax> items) ParseOptionalExtendsBody()
    {
        var items = new List<LetSyntax>();
        if (!TryMatch(TokenKind.LBrace))
        {
            return (false, items);
        }

        while (!TryMatch(TokenKind.RBrace))
        {
            if (!TryMatch(TokenKind.LetKeyword))
            {
                throw Error("Expected 'let' or '}' in an 'extends' body.");
            }

            var name = ExpectName("Expected a field name after 'let'.").Text;
            Expect(TokenKind.Equal, "Expected '=' after the field name.");
            var value = ParseExpression();
            Expect(TokenKind.Semicolon, "Expected ';' after the let override.");
            items.Add(new LetSyntax(name, value));
        }

        return (true, items);
    }

    /// <summary>
    /// Parses the schema base list after the colon in an <c>extends</c> declaration.
    /// </summary>
    /// <returns>The parsed schema bases.</returns>
    private IReadOnlyList<BaseSyntax> ParseExtendsBases()
    {
        var bases = new List<BaseSyntax>();
        do
        {
            var name = ExpectName("Expected a schema class name after ':'.").Text;
            bases.Add(new BaseSyntax(name, ParseOptionalArgumentList()));
        }
        while (TryMatch(TokenKind.Comma) && !Is(TokenKind.LBrace) && !Is(TokenKind.Semicolon));

        return bases;
    }

    /// <summary>
    /// Parses one item inside a class or definition body.
    /// </summary>
    /// <returns>The parsed body item.</returns>
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

    /// <summary>
    /// Parses a type name, preserving any nested angle-bracketed suffix as raw text.
    /// </summary>
    /// <returns>The parsed type name text.</returns>
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

    /// <summary>
    /// Parses an expression, currently treating <c>#</c> concatenation as the only infix operator.
    /// </summary>
    /// <returns>The parsed expression syntax node.</returns>
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

    /// <summary>
    /// Parses a primary expression before postfix field access and subscripts are applied.
    /// </summary>
    /// <returns>The parsed expression syntax node.</returns>
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

    /// <summary>
    /// Parses immediately adjacent string or code-block literals as chained TableGen concatenations.
    /// </summary>
    /// <param name="left">The already-parsed leftmost literal expression.</param>
    /// <returns>The combined concatenation expression.</returns>
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

    /// <summary>
    /// Applies postfix field access and subscript operators to an already-parsed primary expression.
    /// </summary>
    /// <param name="expr">The base expression.</param>
    /// <returns>The expression with any postfix operators applied.</returns>
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

    /// <summary>
    /// Parses a bang operator expression, including the special syntactic forms for operators like <c>!foldl</c> and <c>!cond</c>.
    /// </summary>
    /// <param name="operatorName">The operator name without the leading <c>!</c>.</param>
    /// <returns>The parsed bang expression syntax node.</returns>
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

    /// <summary>
    /// Checks whether the current token has the requested kind.
    /// </summary>
    /// <param name="kind">The token kind to test.</param>
    /// <returns><see langword="true"/> when the current token matches; otherwise <see langword="false"/>.</returns>
    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    /// <summary>
    /// Parses the raw type argument text inside a bang operator type argument list.
    /// </summary>
    /// <returns>The parsed type argument text.</returns>
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

    /// <summary>
    /// Gets the current token, or the final end-of-file token when the parser has advanced past the end.
    /// </summary>
    private Token Current => position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];

    /// <summary>
    /// Consumes the current token if it matches the requested kind.
    /// </summary>
    /// <param name="kind">The token kind to match.</param>
    /// <returns><see langword="true"/> when a token was consumed; otherwise <see langword="false"/>.</returns>
    private bool TryMatch(TokenKind kind)
    {
        if (!Is(kind))
        {
            return false;
        }

        position++;
        return true;
    }

    /// <summary>
    /// Consumes the current token if it matches the requested kind and returns it.
    /// </summary>
    /// <param name="kind">The token kind to match.</param>
    /// <param name="token">Receives the consumed token.</param>
    /// <returns><see langword="true"/> when a token was consumed; otherwise <see langword="false"/>.</returns>
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

    /// <summary>
    /// Requires that the current token have the requested kind.
    /// </summary>
    /// <param name="kind">The required token kind.</param>
    /// <param name="message">The parse error to throw if the token does not match.</param>
    /// <returns>The consumed token.</returns>
    private Token Expect(TokenKind kind, string message)
    {
        if (!Is(kind))
        {
            throw Error(message);
        }

        return tokens[position++];
    }

    /// <summary>
    /// Requires that the current token be usable as a TableGen name.
    /// </summary>
    /// <param name="message">The parse error to throw if no name token is present.</param>
    /// <returns>The consumed name token.</returns>
    private Token ExpectName(string message)
    {
        if (TryMatchName(out var token))
        {
            return token;
        }

        throw Error(message);
    }

    /// <summary>
    /// Consumes the current token when it is one of the token kinds allowed to act as a TableGen name.
    /// </summary>
    /// <param name="token">Receives the consumed token.</param>
    /// <returns><see langword="true"/> when a name token was consumed; otherwise <see langword="false"/>.</returns>
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

    /// <summary>
    /// Determines whether a token kind is accepted in name positions.
    /// </summary>
    /// <param name="kind">The token kind to classify.</param>
    /// <returns><see langword="true"/> when the token kind can act as a name; otherwise <see langword="false"/>.</returns>
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

    /// <summary>
    /// Consumes the current token without validating its kind.
    /// </summary>
    /// <returns>The consumed token.</returns>
    private Token Consume()
    {
        return position < tokens.Count ? tokens[position++] : tokens[tokens.Count - 1];
    }

    /// <summary>
    /// Creates a parse exception anchored at the current token.
    /// </summary>
    /// <param name="message">The parse error message.</param>
    /// <returns>The constructed parse exception.</returns>
    private ParseException Error(string message)
    {
        var token = Current;
        return new ParseException(new Diagnostic(message, token.Line, token.Column, sourceFilePath));
    }
}
