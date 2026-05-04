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
    /// <summary>
    /// Attempts to interpret a canonical builtin integer type name such as <c>i32</c>, <c>si64</c>, or <c>ui8</c>.
    /// </summary>
    public static bool TryParseName(string text, out IntegerTypeSignedness signedness, out int width)
    {
        var parsed = IntegerTypeName.TryParse(text, out var parsedSignedness, out width);
        signedness = parsedSignedness switch
        {
            IntegerTypeName.Kind.Signed => IntegerTypeSignedness.Signed,
            IntegerTypeName.Kind.Unsigned => IntegerTypeSignedness.Unsigned,
            _ => IntegerTypeSignedness.Signless,
        };

        return parsed;
    }

    /// <summary>
    /// Formats a builtin integer type name from width and signedness.
    /// </summary>
    public static string FormatName(int width, IntegerTypeSignedness signedness)
        => IntegerTypeName.Format(width, signedness switch
        {
            IntegerTypeSignedness.Signed => IntegerTypeName.Kind.Signed,
            IntegerTypeSignedness.Unsigned => IntegerTypeName.Kind.Unsigned,
            _ => IntegerTypeName.Kind.Signless,
        });

    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var nameToken))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        if (!TryParseName(nameToken.Text, out var signedness, out var width))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        return ParseResult<TypeSyntax>.Success(new BuiltinIntegerTypeSyntax(nameToken, signedness, width));
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, Binder binder)
    {
        if (syntax is not BuiltinIntegerTypeSyntax integerSyntax)
        {
            throw new InvalidOperationException("Builtin integer types require builtin integer syntax.");
        }

        return new IntegerType(integerSyntax.Width, integerSyntax.Signedness, integerSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type is IntegerType integerType)
        {
            return new BuiltinIntegerTypeSyntax(
                TokenFactory.Identifier(FormatName(integerType.Width, integerType.Signedness)),
                integerType.Signedness,
                integerType.Width);
        }

        return type.Syntax ?? throw new InvalidOperationException("Integer types require syntax to rebuild their assembly form.");
    }

}
