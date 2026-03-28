namespace TableGen.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using TableGen.Evaluation;
using Xunit;

public sealed class TableGenEvaluationTests
{
    [Fact]
    public void EvaluatesInheritanceTemplateArgumentsAndLets()
    {
        const string source =
            "class Base<int width, string name = \"anon\"> {\n" +
            "  int Width = width;\n" +
            "  string Name = name;\n" +
            "  bit Enabled = 0;\n" +
            "};\n" +
            "def Example : Base<8> {\n" +
            "  let Enabled = 1;\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("Example", record.Name);
        Assert.Equal(8, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal("anon", Assert.IsType<StringValue>(record.GetField("Name")).Value);
        Assert.True(Assert.IsType<BitValue>(record.GetField("Enabled")).Value);
    }

    [Fact]
    public void EvaluatesListsAndNestedTemplateInstantiation()
    {
        const string source =
            "class Numbers<list<int> values> {\n" +
            "  list<int> Values = values;\n" +
            "};\n" +
            "class Wrapper<list<int> inner> : Numbers<inner> {\n" +
            "  string Tag = \"wrapped\";\n" +
            "};\n" +
            "def Example : Wrapper<[1, 2, 3]>;";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);
        var values = Assert.IsType<ListValue>(record.GetField("Values"));

        Assert.Equal(new[] { 1, 2, 3 }, values.Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
        Assert.Equal("wrapped", Assert.IsType<StringValue>(record.GetField("Tag")).Value);
        Assert.Equal(["Wrapper", "Numbers"], record.BaseClasses);
    }

    [Fact]
    public void EvaluatesEmptyListLiterals()
    {
        const string source =
            "def Example {\n" +
            "  list<int> Values = [];\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Empty(Assert.IsType<ListValue>(record.GetField("Values")).Items);
    }

    [Fact]
    public void EvaluatesBitInitializersAndOverridesFromIntegersAndBooleans()
    {
        const string source =
            "def Example {\n" +
            "  bit First = 1;\n" +
            "  bit Second = true;\n" +
            "  bit Third = false;\n" +
            "  let First = 0;\n" +
            "  let Second = false;\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.False(Assert.IsType<BitValue>(record.GetField("First")).Value);
        Assert.False(Assert.IsType<BitValue>(record.GetField("Second")).Value);
        Assert.False(Assert.IsType<BitValue>(record.GetField("Third")).Value);
    }

    [Fact]
    public void LetsPreserveNonBitFieldTypes()
    {
        const string source =
            "def Example {\n" +
            "  int Width = 4;\n" +
            "  string Name = \"old\";\n" +
            "  let Width = 9;\n" +
            "  let Name = \"new\";\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(9, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal("new", Assert.IsType<StringValue>(record.GetField("Name")).Value);
    }

    [Fact]
    public void EvaluatesThroughThePublicDocumentApi()
    {
        const string source =
            "def First { int Width = 1; };\n" +
            "def Second { int Width = 2; };";

        var document = TableGenDocument.Parse(source).Evaluate();

        Assert.Equal(2, document.Records.Count);
        Assert.Equal(1, Assert.IsType<IntegerValue>(document.Records[0].GetField("Width")).Value);
        Assert.Equal(2, Assert.IsType<IntegerValue>(document.Records[1].GetField("Width")).Value);
    }

    [Fact]
    public void EvaluatesCodeBlockStrings()
    {
        const string source =
            "def Example {\n" +
            "  string Description = [{Hello from a code block.}];\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("Hello from a code block.", Assert.IsType<StringValue>(record.GetField("Description")).Value);
    }

    [Fact]
    public void ReportsMissingTemplateArgumentsWhenNoDefaultExists()
    {
        const string source =
            "class Base<int width> { int Width = width; };\n" +
            "def Example : Base<>;";

        var document = TableGenDocument.Parse(source);
        var exception = Assert.Throws<InvalidOperationException>(() => document.Evaluate());

        Assert.Contains("Missing value for template parameter 'width'", exception.Message);
    }

    [Fact]
    public void ReportsUnknownBaseClasses()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => TableGenDocument.Parse("def Example : MissingBase;").Evaluate());

        Assert.Contains("Unknown TableGen class 'MissingBase'.", exception.Message);
    }

    [Fact]
    public void ReportsUnknownIdentifiers()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => TableGenDocument.Parse("def Example { int Width = missing; };").Evaluate());

        Assert.Contains("Unknown TableGen identifier 'missing'.", exception.Message);
    }
}
