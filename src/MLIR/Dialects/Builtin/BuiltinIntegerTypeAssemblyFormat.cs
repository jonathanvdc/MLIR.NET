namespace MLIR.Dialects.Builtin;

using System;
using MLIR.Semantics;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rebuilds builtin integer types such as <c>i32</c> and <c>si64</c>.
/// </summary>
public sealed class BuiltinIntegerTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var nameToken))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        if (!TryParseIntegerName(nameToken.Text, out var signedness, out var width))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        return ParseResult<TypeSyntax>.Success(new BuiltinIntegerTypeSyntax(nameToken, signedness, width));
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
    {
        if (syntax is not BuiltinIntegerTypeSyntax integerSyntax)
        {
            throw new InvalidOperationException("Builtin integer types require builtin integer syntax.");
        }

        return new IntegerType(new TypeReferenceConstructionContext(
            integerSyntax,
            integerSyntax.NameToken.Text,
            definition,
            integerSyntax.Location));
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type is IntegerType integerType)
        {
            return new BuiltinIntegerTypeSyntax(
                TokenFactory.Identifier(FormatIntegerName(integerType.Width, integerType.Signedness)),
                integerType.Signedness,
                integerType.Width);
        }

        return type.Syntax ?? throw new InvalidOperationException("Integer types require syntax to rebuild their assembly form.");
    }

    private static bool TryParseIntegerName(string text, out IntegerTypeSignedness signedness, out int width)
    {
        signedness = IntegerTypeSignedness.Signless;
        width = 0;

        if (text.Length < 2)
        {
            return false;
        }

        var widthText = text;
        if (text.StartsWith("si", StringComparison.Ordinal))
        {
            signedness = IntegerTypeSignedness.Signed;
            widthText = text.Substring(2);
        }
        else if (text.StartsWith("ui", StringComparison.Ordinal))
        {
            signedness = IntegerTypeSignedness.Unsigned;
            widthText = text.Substring(2);
        }
        else if (text[0] == 'i')
        {
            widthText = text.Substring(1);
        }
        else
        {
            return false;
        }

        return int.TryParse(widthText, out width);
    }

    private static string FormatIntegerName(int width, IntegerTypeSignedness signedness)
    {
        return signedness switch
        {
            IntegerTypeSignedness.Signed => "si" + width,
            IntegerTypeSignedness.Unsigned => "ui" + width,
            _ => "i" + width,
        };
    }
}
