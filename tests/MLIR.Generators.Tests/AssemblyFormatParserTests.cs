namespace MLIR.Generators.Tests;

using System.Collections.Generic;
using MLIR.ODS;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;
using Xunit;

public sealed class AssemblyFormatParserTests
{
    // -----------------------------------------------------------------------
    // Simple variable + attr-dict
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesSimpleVariableAndAttrDict()
    {
        var model = AssemblyFormatParser.Parse("$value attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var variable = Assert.IsType<VariableChunk>(model.Elements[0]);
        Assert.Equal("value", variable.Name);
        Assert.False(variable.IsAnchor);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // Variable, literal, type directive, attr-dict
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesVariableLiteralTypeAndAttrDict()
    {
        var model = AssemblyFormatParser.Parse("$value `:` type($value) attr-dict");

        Assert.Equal(4, model.Elements.Count);
        var variable = Assert.IsType<VariableChunk>(model.Elements[0]);
        Assert.Equal("value", variable.Name);
        var literal = Assert.IsType<LiteralChunk>(model.Elements[1]);
        Assert.Equal(TokenKind.Colon, Assert.IsType<PunctuationLiteral>(Assert.Single(literal.Value)).TokenKind);
        var typeDir = Assert.IsType<TypeDirectiveChunk>(model.Elements[2]);
        var operand = Assert.IsType<VariableOperand>(typeDir.Operand);
        Assert.Equal("value", operand.Name);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[3]);
    }

    // -----------------------------------------------------------------------
    // Two variables + comma literal + attr-dict
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesTwoVariablesWithLiteral()
    {
        var model = AssemblyFormatParser.Parse("$lhs `,` $rhs attr-dict");

        Assert.Equal(4, model.Elements.Count);
        Assert.Equal("lhs", Assert.IsType<VariableChunk>(model.Elements[0]).Name);
        Assert.Equal(TokenKind.Comma, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(model.Elements[1]).Value)).TokenKind);
        Assert.Equal("rhs", Assert.IsType<VariableChunk>(model.Elements[2]).Name);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[3]);
    }

    // -----------------------------------------------------------------------
    // functional-type directive
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesFunctionalTypeDirective()
    {
        var model = AssemblyFormatParser.Parse("$inputs `:` functional-type($inputs, results) attr-dict");

        Assert.Equal(4, model.Elements.Count);
        Assert.Equal("inputs", Assert.IsType<VariableChunk>(model.Elements[0]).Name);
        Assert.Equal(TokenKind.Colon, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(model.Elements[1]).Value)).TokenKind);
        var ft = Assert.IsType<FunctionalTypeDirectiveChunk>(model.Elements[2]);
        Assert.Equal("inputs", Assert.IsType<VariableOperand>(ft.Inputs).Name);
        Assert.Equal("results", Assert.IsType<VariableOperand>(ft.Outputs).Name);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[3]);
    }

    // -----------------------------------------------------------------------
    // Optional group with variable anchor
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesOptionalGroupWithVariableAnchor()
    {
        var model = AssemblyFormatParser.Parse("(`,` $rhs^)? attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var group = Assert.IsType<OptionalGroup>(model.Elements[0]);
        Assert.Equal("rhs", group.AnchorName);
        Assert.Null(group.ElseElements);
        Assert.Equal(2, group.ThenElements.Count);
        Assert.Equal(TokenKind.Comma, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(group.ThenElements[0]).Value)).TokenKind);
        var anchor = Assert.IsType<VariableChunk>(group.ThenElements[1]);
        Assert.Equal("rhs", anchor.Name);
        Assert.True(anchor.IsAnchor);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // Optional group with then/else branches and directive anchor
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesOptionalGroupWithElseBranchAndDirectiveAnchor()
    {
        var model = AssemblyFormatParser.Parse(
            "(`:` type($value)^):(`:` qualified(type($fallback)))? attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var group = Assert.IsType<OptionalGroup>(model.Elements[0]);
        Assert.Equal("value", group.AnchorName);
        Assert.NotNull(group.ElseElements);

        // Then branch: `:` type($value)^
        Assert.Equal(2, group.ThenElements.Count);
        Assert.Equal(TokenKind.Colon, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(group.ThenElements[0]).Value)).TokenKind);
        var typeDir = Assert.IsType<TypeDirectiveChunk>(group.ThenElements[1]);
        Assert.Equal("value", Assert.IsType<VariableOperand>(typeDir.Operand).Name);

        // Else branch: `:` qualified(type($fallback))
        Assert.Equal(2, group.ElseElements!.Count);
        Assert.Equal(TokenKind.Colon, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(group.ElseElements[0]).Value)).TokenKind);
        var qualDir = Assert.IsType<QualifiedDirectiveChunk>(group.ElseElements[1]);
        var innerType = Assert.IsType<TypeDirectiveOperand>(qualDir.Operand);
        Assert.Equal("fallback", Assert.IsType<VariableOperand>(innerType.Operand).Name);

        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // Custom directive (single parameter)
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesCustomDirectiveWithSingleParameter()
    {
        var model = AssemblyFormatParser.Parse("custom             ($value) attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var custom = Assert.IsType<CustomDirectiveChunk>(model.Elements[0]);
        Assert.Equal("custom", custom.Name);
        Assert.Equal("value", Assert.IsType<VariableOperand>(Assert.Single(custom.Parameters)).Name);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // Custom directive (ref and type parameters)
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesCustomDirectiveWithRefAndTypeParameters()
    {
        var model = AssemblyFormatParser.Parse("custom     (ref($value), type($value)) attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var custom = Assert.IsType<CustomDirectiveChunk>(model.Elements[0]);
        Assert.Equal("custom", custom.Name);
        Assert.Equal(2, custom.Parameters.Count);
        var refOp = Assert.IsType<RefDirectiveOperand>(custom.Parameters[0]);
        Assert.Equal("value", Assert.IsType<VariableOperand>(refOp.Operand).Name);
        var typeOp = Assert.IsType<TypeDirectiveOperand>(custom.Parameters[1]);
        Assert.Equal("value", Assert.IsType<VariableOperand>(typeOp.Operand).Name);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // oilist with two clauses
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesOilistWithTwoClauses()
    {
        var model = AssemblyFormatParser.Parse(
            "oilist(`stride` $stride | `padding` $padding) attr-dict");

        Assert.Equal(2, model.Elements.Count);
        var oilist = Assert.IsType<OilistDirectiveChunk>(model.Elements[0]);
        Assert.Equal(2, oilist.Clauses.Count);

        Assert.Equal("stride", oilist.Clauses[0].Keyword);
        Assert.Equal("stride", Assert.IsType<OilistVariableElement>(Assert.Single(oilist.Clauses[0].Elements)).Name);

        Assert.Equal("padding", oilist.Clauses[1].Keyword);
        Assert.Equal("padding", Assert.IsType<OilistVariableElement>(Assert.Single(oilist.Clauses[1].Elements)).Name);

        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // oilist with type directive elements, no trailing attr-dict
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesOilistWithTypeDirectiveElementsNoAttrDict()
    {
        var model = AssemblyFormatParser.Parse(
            "oilist(`dtype` type($value) | `axis` $axis)");

        var oilist = Assert.IsType<OilistDirectiveChunk>(Assert.Single(model.Elements));
        Assert.Equal(2, oilist.Clauses.Count);

        Assert.Equal("dtype", oilist.Clauses[0].Keyword);
        var typeEl = Assert.IsType<OilistTypeDirectiveElement>(Assert.Single(oilist.Clauses[0].Elements));
        Assert.Equal("value", Assert.IsType<VariableOperand>(typeEl.Operand).Name);

        Assert.Equal("axis", oilist.Clauses[1].Keyword);
        Assert.Equal("axis", Assert.IsType<OilistVariableElement>(Assert.Single(oilist.Clauses[1].Elements)).Name);
    }

    // -----------------------------------------------------------------------
    // regions directive
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesRegionsAndAttrDict()
    {
        var model = AssemblyFormatParser.Parse("regions attr-dict");

        Assert.Equal(2, model.Elements.Count);
        Assert.IsType<RegionsDirectiveChunk>(model.Elements[0]);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // successors directive
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesSuccessorsAndAttrDict()
    {
        var model = AssemblyFormatParser.Parse("successors attr-dict");

        Assert.Equal(2, model.Elements.Count);
        Assert.IsType<SuccessorsDirectiveChunk>(model.Elements[0]);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // attr-dict-with-keyword directive
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesAttrDictWithKeyword()
    {
        var model = AssemblyFormatParser.Parse("$value attr-dict-with-keyword");

        Assert.Equal(2, model.Elements.Count);
        Assert.Equal("value", Assert.IsType<VariableChunk>(model.Elements[0]).Name);
        Assert.IsType<AttrDictWithKeywordDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // prop-dict directive
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesPropDict()
    {
        var model = AssemblyFormatParser.Parse("$value prop-dict");

        Assert.Equal(2, model.Elements.Count);
        Assert.Equal("value", Assert.IsType<VariableChunk>(model.Elements[0]).Name);
        Assert.IsType<PropDictDirectiveChunk>(model.Elements[1]);
    }

    // -----------------------------------------------------------------------
    // operands and results directives
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesOperandsAndResultsDirectives()
    {
        var model = AssemblyFormatParser.Parse("operands results attr-dict");

        Assert.Equal(3, model.Elements.Count);
        Assert.IsType<OperandsDirectiveChunk>(model.Elements[0]);
        Assert.IsType<ResultsDirectiveChunk>(model.Elements[1]);
        Assert.IsType<AttrDictDirectiveChunk>(model.Elements[2]);
    }

    // -----------------------------------------------------------------------
    // Empty format
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesEmptyFormat()
    {
        var model = AssemblyFormatParser.Parse("");

        Assert.Empty(model.Elements);
    }

    // -----------------------------------------------------------------------
    // Whitespace-only format
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsesWhitespaceOnlyFormat()
    {
        var model = AssemblyFormatParser.Parse("   ");

        Assert.Empty(model.Elements);
    }

    // -----------------------------------------------------------------------
    // Error cases
    // -----------------------------------------------------------------------

    [Fact]
    public void ThrowsOnUnclosedLiteral()
    {
        Assert.Throws<FormatException>(() => AssemblyFormatParser.Parse("`unclosed"));
    }

    [Fact]
    public void ThrowsOnUnclosedOptionalGroup()
    {
        Assert.Throws<FormatException>(() => AssemblyFormatParser.Parse("($value attr-dict"));
    }

    [Fact]
    public void ThrowsOnMissingOptionalGroupQuestionMark()
    {
        Assert.Throws<FormatException>(() => AssemblyFormatParser.Parse("($value^)"));
    }

    [Fact]
    public void ThrowsOnUnknownDirectiveOperand()
    {
        Assert.Throws<FormatException>(() => AssemblyFormatParser.Parse("type(unknown)"));
    }

    // -----------------------------------------------------------------------
    // Literal.Parse examples from the problem statement
    // -----------------------------------------------------------------------

    [Fact]
    public void LiteralParse_Comma()
    {
        var literals = Literal.Parse(",");
        var p = Assert.IsType<PunctuationLiteral>(Assert.Single(literals));
        Assert.Equal(TokenKind.Comma, p.TokenKind);
    }

    [Fact]
    public void LiteralParse_LParen()
    {
        var literals = Literal.Parse("(");
        var p = Assert.IsType<PunctuationLiteral>(Assert.Single(literals));
        Assert.Equal(TokenKind.LParen, p.TokenKind);
    }

    [Fact]
    public void LiteralParse_RParen()
    {
        var literals = Literal.Parse(")");
        var p = Assert.IsType<PunctuationLiteral>(Assert.Single(literals));
        Assert.Equal(TokenKind.RParen, p.TokenKind);
    }

    [Fact]
    public void LiteralParse_Arrow()
    {
        var literals = Literal.Parse("->");
        var p = Assert.IsType<PunctuationLiteral>(Assert.Single(literals));
        Assert.Equal(TokenKind.Arrow, p.TokenKind);
    }

    [Fact]
    public void LiteralParse_LBracket()
    {
        var literals = Literal.Parse("[");
        var p = Assert.IsType<PunctuationLiteral>(Assert.Single(literals));
        Assert.Equal(TokenKind.LBracket, p.TokenKind);
    }

    [Fact]
    public void LiteralParse_Keyword_else()
    {
        var literals = Literal.Parse("else");
        var k = Assert.IsType<KeywordLiteral>(Assert.Single(literals));
        Assert.Equal("else", k.Spelling);
    }

    [Fact]
    public void LiteralParse_Keyword_to()
    {
        var literals = Literal.Parse("to");
        var k = Assert.IsType<KeywordLiteral>(Assert.Single(literals));
        Assert.Equal("to", k.Spelling);
    }

    [Fact]
    public void LiteralParse_Keyword_in()
    {
        var literals = Literal.Parse("in");
        var k = Assert.IsType<KeywordLiteral>(Assert.Single(literals));
        Assert.Equal("in", k.Spelling);
    }

    [Fact]
    public void LiteralParse_Newline()
    {
        var literals = Literal.Parse(@"\n");
        Assert.IsType<NewlineLiteral>(Assert.Single(literals));
    }

    [Fact]
    public void LiteralParse_Empty()
    {
        var literals = Literal.Parse("");
        Assert.IsType<EmptyLiteral>(Assert.Single(literals));
    }

    [Fact]
    public void LiteralParse_SingleSpace()
    {
        var literals = Literal.Parse(" ");
        var w = Assert.IsType<WhitespaceLiteral>(Assert.Single(literals));
        Assert.Equal(" ", w.Spaces);
    }

    [Fact]
    public void LiteralParse_TwoSpaces()
    {
        var literals = Literal.Parse("  ");
        var w = Assert.IsType<WhitespaceLiteral>(Assert.Single(literals));
        Assert.Equal("  ", w.Spaces);
    }

    [Fact]
    public void LiteralParse_TwoNewlines()
    {
        var literals = Literal.Parse(@"\n\n");
        Assert.Equal(2, literals.Count);
        Assert.IsType<NewlineLiteral>(literals[0]);
        Assert.IsType<NewlineLiteral>(literals[1]);
    }

    [Fact]
    public void LiteralParse_ArrowLParen()
    {
        var literals = Literal.Parse("->(");
        Assert.Equal(2, literals.Count);
        Assert.Equal(TokenKind.Arrow, Assert.IsType<PunctuationLiteral>(literals[0]).TokenKind);
        Assert.Equal(TokenKind.LParen, Assert.IsType<PunctuationLiteral>(literals[1]).TokenKind);
    }

    [Fact]
    public void LiteralParse_RParenComma()
    {
        var literals = Literal.Parse("),");
        Assert.Equal(2, literals.Count);
        Assert.Equal(TokenKind.RParen, Assert.IsType<PunctuationLiteral>(literals[0]).TokenKind);
        Assert.Equal(TokenKind.Comma, Assert.IsType<PunctuationLiteral>(literals[1]).TokenKind);
    }

    [Fact]
    public void LiteralParse_Keyword_x()
    {
        var literals = Literal.Parse("x");
        var k = Assert.IsType<KeywordLiteral>(Assert.Single(literals));
        Assert.Equal("x", k.Spelling);
    }

    [Fact]
    public void LiteralParse_Keyword_yield()
    {
        var literals = Literal.Parse("yield");
        var k = Assert.IsType<KeywordLiteral>(Assert.Single(literals));
        Assert.Equal("yield", k.Spelling);
    }
}
