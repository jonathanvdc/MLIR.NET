namespace MLIR.Tests;

using MLIR.Text;
using Xunit;

public sealed class MlirErrorTests
{
    [Fact]
    public void ThrowsHelpfulExceptionForInvalidInput()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi\"(%lhs, %rhs"));

        Assert.Contains("operand list", exception.Message);
    }

    [Fact]
    public void ReportsLexerErrorForUnexpectedCharacter()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi\"(%lhs) !"));

        Assert.Equal("Unexpected character '!'.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(20, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForUnterminatedStringLiteral()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi"));

        Assert.Equal("Unterminated string literal.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForMissingSsaNameAfterPercent()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("% = \"test.op\"()"));

        Assert.Equal("Expected a name after '%'.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForMissingBlockNameAfterCaret()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"test.region\"() {\n^:\n}"));

        Assert.Equal("Expected a name after '^'.", exception.Diagnostic.Message);
        Assert.Equal(2, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingOperationName()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("%0 = (%lhs)"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(6, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingRegionTerminator()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"scf.if\"(%cond) {\n  \"func.return\"() : () -> ()"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(2, exception.Diagnostic.Line);
        Assert.Equal(29, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingAttributeName()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"test.op\"() {= 42 : i32} : () -> ()"));

        Assert.Equal("Expected an attribute name.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(14, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingBlockLabelName()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"test.region\"() {\n^bb0(%arg0: i32):\n  \"cf.br\"() [^] : () -> ()\n}"));

        Assert.Equal("Expected a name after '^'.", exception.Diagnostic.Message);
        Assert.Equal(3, exception.Diagnostic.Line);
        Assert.Equal(14, exception.Diagnostic.Column);
    }
}
