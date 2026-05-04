namespace MLIR.Dialects.Builtin;

using System;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds a scalar builtin floating-point type such as <c>f32</c> or <c>bf16</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each generated scalar float <c>TypeDef</c> class supplies a constructor delegate so the shared
/// <see cref="BuiltinScalarFloatTypeAssemblyFormat"/> can produce the correct concrete
/// <see cref="TypeReference"/> subclass without knowing its type at compile time.
/// </para>
/// </remarks>
public sealed class BuiltinScalarFloatTypeAssemblyFormat : ITypeAssemblyFormat
{
    private readonly Func<BuiltinFloatTypeSyntax?, TypeReference> _create;

    /// <summary>
    /// Returns whether the supplied identifier is one of MLIR's canonical builtin float spellings.
    /// </summary>
    public static bool IsBuiltinFloatName(string text)
    {
        if (text is "bf16" or "tf32")
        {
            return true;
        }

        if (text.Length < 2 || text[0] != 'f' || !char.IsDigit(text[1]))
        {
            return false;
        }

        var index = 1;
        while (index < text.Length && char.IsDigit(text[index]))
        {
            index++;
        }

        if (index == text.Length)
        {
            return true;
        }

        for (; index < text.Length; index++)
        {
            if (!char.IsLetterOrDigit(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltinScalarFloatTypeAssemblyFormat"/> class.
    /// </summary>
    /// <param name="create">
    /// A delegate that constructs the concrete float type from an optional parsed syntax node.
    /// </param>
    public BuiltinScalarFloatTypeAssemblyFormat(Func<BuiltinFloatTypeSyntax?, TypeReference> create)
    {
        _create = create;
    }

    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(ParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var nameToken))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        return IsBuiltinFloatName(nameToken.Text)
            ? ParseResult<TypeSyntax>.Success(new BuiltinFloatTypeSyntax(nameToken))
            : ParseResult<TypeSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, Binder binder)
    {
        return _create(syntax as BuiltinFloatTypeSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is BuiltinFloatTypeSyntax existing)
        {
            return existing;
        }

        var name = type.Name ?? throw new InvalidOperationException(
            $"Cannot rebuild assembly syntax for a float type with no canonical name.");
        return new BuiltinFloatTypeSyntax(TokenFactory.Identifier(name));
    }
}
