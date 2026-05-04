namespace MLIR.Tests;

using MLIR.Text;
using Xunit;

public sealed class ErrorTests
{
    [Fact]
    public void ThrowsHelpfulExceptionForInvalidInput()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"arith.addi\"(%lhs, %rhs"));

        Assert.Contains("operand list", exception.Message);
    }

    [Fact]
    public void ReportsLexerErrorForUnexpectedCharacter()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"arith.addi\"(%lhs) ;"));

        Assert.Equal("Unexpected character ';'.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(20, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForUnterminatedStringLiteral()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"arith.addi"));

        Assert.Equal("Unterminated string literal.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForMissingSsaNameAfterPercent()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("% = \"test.op\"()"));

        Assert.Equal("Expected a name after '%'.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForMissingBlockNameAfterCaret()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"test.region\"() {\n^:\n}"));

        Assert.Equal("Expected a name after '^'.", exception.Diagnostic.Message);
        Assert.Equal(2, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingOperationName()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("%0 = (%lhs)"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(6, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingRegionTerminator()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"scf.if\"(%cond) {\n  \"func.return\"() : () -> ()"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(2, exception.Diagnostic.Line);
        Assert.Equal(29, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingAttributeName()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"test.op\"() {= 42 : i32} : () -> ()"));

        Assert.Equal("Expected an attribute name.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(14, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingBlockLabelName()
    {
        var exception = Assert.Throws<ParseException>(() => Parser.ParseModule("\"test.region\"() {\n^bb0(%arg0: i32):\n  \"cf.br\"() [^] : () -> ()\n}"));

        Assert.Equal("Expected a name after '^'.", exception.Diagnostic.Message);
        Assert.Equal(3, exception.Diagnostic.Line);
        Assert.Equal(14, exception.Diagnostic.Column);
    }

    [Fact]
    public void TryParseModuleReturnsLexerDiagnosticWithoutThrowing()
    {
        var success = Parser.TryParseModule("\"arith.addi\"(%lhs) ;", out var module, out var diagnostic);

        Assert.False(success);
        Assert.Null(module);
        Assert.NotNull(diagnostic);
        Assert.Equal("Unexpected character ';'.", diagnostic!.Message);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(20, diagnostic.Column);
    }

    [Fact]
    public void TryParseModuleReturnsParserDiagnosticWithoutThrowing()
    {
        var success = Parser.TryParseModule("%0 = (%lhs)", out var module, out var diagnostic);

        Assert.False(success);
        Assert.Null(module);
        Assert.NotNull(diagnostic);
        Assert.Equal("Expected an operation name.", diagnostic!.Message);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(6, diagnostic.Column);
    }

    [Fact]
    public void TryParseTypeReturnsDiagnosticWithoutThrowing()
    {
        var success = Parser.TryParseType("(i32", out var type, out var diagnostic);

        Assert.False(success);
        Assert.Null(type);
        Assert.NotNull(diagnostic);
        Assert.Contains("type list", diagnostic!.Message);
    }

    [Fact]
    public void TryParseAttributeValueReturnsDiagnosticWithoutThrowing()
    {
        var success = Parser.TryParseAttributeValue("[1, ", out var attribute, out var diagnostic);

        Assert.False(success);
        Assert.Null(attribute);
        Assert.NotNull(diagnostic);
        Assert.Contains("Expected an attribute value", diagnostic!.Message);
    }

    [Fact]
    public void DocumentTryParseReturnsDiagnosticWithoutThrowing()
    {
        var success = Document.TryParse("\"arith.addi", out var document, out var diagnostic);

        Assert.False(success);
        Assert.Null(document);
        Assert.NotNull(diagnostic);
        Assert.Equal("Unterminated string literal.", diagnostic!.Message);
    }
}
