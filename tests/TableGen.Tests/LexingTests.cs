namespace TableGen.Tests;

using TableGen.Evaluation;
using TableGen.Syntax;
using TableGen.Text;
using Xunit;

public sealed class LexingTests
{
    [Fact]
    public void IgnoresLineAndBlockComments()
    {
        const string source =
            "// comment\n" +
            "class Base<int width> { /* body */ int Width = width; }; \n" +
            "def Example : Base<4>;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(4, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
    }

    [Fact]
    public void ParsesStringLiteralEscapes()
    {
        const string source =
            "def Example {\n" +
            "  string Value = \"slash\\\\quote\\\"line\\ncarriage\\r tab\\tend\";\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("slash\\quote\"line\ncarriage\r tab\tend", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void SkipsPreprocessorDirectives()
    {
        const string source =
            "#ifndef SOME_TD\n" +
            "#define SOME_TD\n" +
            "def Example { string Name = \"hello\"; };\n" +
            "#endif // SOME_TD\n";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("hello", Assert.IsType<StringValue>(record.GetField("Name")).Value);
    }

    [Fact]
    public void LexesHashConcatAsHashToken()
    {
        const string source = "def Example { string Value = \"a\" # \"b\"; };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);

        Assert.IsType<ConcatSyntax>(field.Initializer);
    }

    [Fact]
    public void DoesNotTreatHashConcatWithoutWhitespaceAsPreprocessor()
    {
        const string source = "def Example { string Value = \"a\"#name; };";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("aname", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void LexesBangKeywordsWithOperatorName()
    {
        const string source = "def Example { string Value = !toupper(\"hello\"); };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var bang = Assert.IsType<BangCallSyntax>(field.Initializer);

        Assert.Equal("toupper", bang.OperatorName);
    }

    [Fact]
    public void ReportsUnterminatedStrings()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("def Bad { string Name = \"oops; };"));

        Assert.Contains("Unterminated string literal", exception.Message);
    }

    [Fact]
    public void ReportsUnterminatedBlockComments()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("/* unterminated"));

        Assert.Contains("Unterminated block comment.", exception.Message);
    }

    [Fact]
    public void ReportsUnexpectedCharacters()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("@"));

        Assert.Contains("Unexpected character '@'.", exception.Message);
    }

    [Fact]
    public void ReportsDanglingSlashThatIsNotAComment()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("/"));

        Assert.Contains("Unexpected character '/'.", exception.Message);
    }

    [Fact]
    public void ReportsBangWithoutIdentifier()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("def Bad { string V = !(; };"));

        Assert.Contains("Expected a bang operator name after '!'.", exception.Message);
    }
}
