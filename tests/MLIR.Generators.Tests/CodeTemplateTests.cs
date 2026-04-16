namespace MLIR.Generators.Tests;

using System.Collections.Generic;
using MLIR.ODS.Model;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="CodeTemplate"/> abstraction.
/// </summary>
public sealed class CodeTemplateTests
{
    // -----------------------------------------------------------------------
    // Placeholder discovery
    // -----------------------------------------------------------------------

    [Fact]
    public void PlaceholderNamesIsEmptyForTemplateWithNoPlaceholders()
    {
        var template = new CodeTemplate("context.TryParseAttributeValueSyntax()", CodeTemplateKind.Expression);
        Assert.Empty(template.PlaceholderNames);
    }

    [Fact]
    public void PlaceholderNamesContainsSinglePlaceholder()
    {
        var template = new CodeTemplate("${parser}.TryParseAttributeValueSyntax()", CodeTemplateKind.Expression);
        Assert.Equal(["parser"], template.PlaceholderNames);
    }

    [Fact]
    public void PlaceholderNamesContainsMultiplePlaceholders()
    {
        var template = new CodeTemplate("${self}.Convert(${context})", CodeTemplateKind.Expression);
        Assert.Equal(["self", "context"], template.PlaceholderNames);
    }

    [Fact]
    public void PlaceholderNamesDeduplicatesRepeatedOccurrences()
    {
        var template = new CodeTemplate("${value} + ${value}", CodeTemplateKind.Expression);
        Assert.Equal(["value"], template.PlaceholderNames);
    }

    [Fact]
    public void PlaceholderNamesPreservesDeclarationOrder()
    {
        var template = new CodeTemplate("${syntax}.A + ${self}.B + ${parser}.C", CodeTemplateKind.Expression);
        Assert.Equal(["syntax", "self", "parser"], template.PlaceholderNames);
    }

    [Fact]
    public void TextWithDollarSignThatIsNotPlaceholderIsIgnored()
    {
        // A bare '$' followed by something other than '{name}' is not a placeholder.
        var template = new CodeTemplate("$_self + something", CodeTemplateKind.Expression);
        Assert.Empty(template.PlaceholderNames);
    }

    // -----------------------------------------------------------------------
    // Render
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderWithNoPlaceholdersReturnsTextUnchanged()
    {
        var template = new CodeTemplate("context.TryParseAttributeValueSyntax()", CodeTemplateKind.Expression);
        var result = template.Render(new Dictionary<string, string>());
        Assert.Equal("context.TryParseAttributeValueSyntax()", result);
    }

    [Fact]
    public void RenderSubstitutesSinglePlaceholder()
    {
        var template = new CodeTemplate("${parser}.TryParseAttributeValueSyntax()", CodeTemplateKind.Expression);
        var result = template.Render("parser", "ctx");
        Assert.Equal("ctx.TryParseAttributeValueSyntax()", result);
    }

    [Fact]
    public void RenderSubstitutesMultiplePlaceholders()
    {
        var template = new CodeTemplate("${self}.Convert(${context})", CodeTemplateKind.Expression);
        var result = template.Render(("self", "storage"), ("context", "builder"));
        Assert.Equal("storage.Convert(builder)", result);
    }

    [Fact]
    public void RenderSubstitutesRepeatedPlaceholder()
    {
        var template = new CodeTemplate("${value} == null ? null : new Wrapper(${value})", CodeTemplateKind.Expression);
        var result = template.Render("value", "x");
        Assert.Equal("x == null ? null : new Wrapper(x)", result);
    }

    [Fact]
    public void RenderIgnoresExtraValuesNotInTemplate()
    {
        var template = new CodeTemplate("${parser}.TryParse()", CodeTemplateKind.Expression);
        var result = template.Render(("parser", "ctx"), ("unused", "should_not_appear"));
        Assert.Equal("ctx.TryParse()", result);
    }

    [Fact]
    public void RenderThrowsWhenRequiredPlaceholderIsMissing()
    {
        var template = new CodeTemplate("${parser}.TryParse(${self})", CodeTemplateKind.Expression);
        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            template.Render("parser", "ctx"));
        Assert.Contains("${self}", ex.Message);
    }

    [Fact]
    public void RenderThrowsWhenValuesIsNull()
    {
        var template = new CodeTemplate("${parser}.TryParse()", CodeTemplateKind.Expression);
        Assert.Throws<System.ArgumentNullException>(() =>
            template.Render(null!));
    }

    // -----------------------------------------------------------------------
    // RequireOnly
    // -----------------------------------------------------------------------

    [Fact]
    public void RequireOnlySucceedsWhenAllPlaceholdersAreAllowed()
    {
        var template = new CodeTemplate("${parser}.TryParse(${self})", CodeTemplateKind.Expression);
        // Should not throw.
        template.RequireOnly("parser", "self", "extra");
    }

    [Fact]
    public void RequireOnlySucceedsForTemplateWithNoPlaceholders()
    {
        var template = new CodeTemplate("context.TryParseAttributeValueSyntax()", CodeTemplateKind.Expression);
        // Should not throw even with an empty allowed list.
        template.RequireOnly();
    }

    [Fact]
    public void RequireOnlyThrowsWhenPlaceholderIsNotAllowed()
    {
        var template = new CodeTemplate("${parser}.TryParse(${context})", CodeTemplateKind.Expression);
        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            template.RequireOnly("parser"));
        Assert.Contains("${context}", ex.Message);
    }

    [Fact]
    public void RequireOnlyThrowsWhenAllowedListIsEmpty()
    {
        var template = new CodeTemplate("${self}.Value", CodeTemplateKind.Expression);
        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            template.RequireOnly());
        Assert.Contains("${self}", ex.Message);
    }

    // -----------------------------------------------------------------------
    // From – normalization
    // -----------------------------------------------------------------------

    [Fact]
    public void FromReturnsNullForNullInput()
    {
        var result = CodeTemplate.From(null, CodeTemplateKind.Expression);
        Assert.Null(result);
    }

    [Fact]
    public void FromReturnsNullForEmptyInput()
    {
        var result = CodeTemplate.From(string.Empty, CodeTemplateKind.Expression);
        Assert.Null(result);
    }

    [Fact]
    public void FromNormalizesParserPlaceholder()
    {
        var result = CodeTemplate.From("$_parser.TryParse()", CodeTemplateKind.Expression);
        Assert.NotNull(result);
        Assert.Equal("${parser}.TryParse()", result!.Text);
        Assert.Equal(["parser"], result.PlaceholderNames);
    }

    [Fact]
    public void FromNormalizesSelfPlaceholder()
    {
        var result = CodeTemplate.From("$_self.Value", CodeTemplateKind.Expression);
        Assert.NotNull(result);
        Assert.Equal("${self}.Value", result!.Text);
        Assert.Equal(["self"], result.PlaceholderNames);
    }

    [Fact]
    public void FromNormalizesSyntaxPlaceholder()
    {
        var result = CodeTemplate.From("((StringAttr)$_syntax).Value", CodeTemplateKind.Expression);
        Assert.NotNull(result);
        Assert.Equal("((StringAttr)${syntax}).Value", result!.Text);
        Assert.Equal(["syntax"], result.PlaceholderNames);
    }

    [Fact]
    public void FromNormalizesZeroPlaceholder()
    {
        var result = CodeTemplate.From("new IntegerAttr($0)", CodeTemplateKind.Expression, new Dictionary<string, string>(StringComparer.Ordinal) { ["0"] = "value" });
        Assert.NotNull(result);
        Assert.Equal("new IntegerAttr(${value})", result!.Text);
        Assert.Equal(["value"], result.PlaceholderNames);
    }

    [Fact]
    public void FromNormalizesCanonicalSyntaxUnchanged()
    {
        const string canonical = "${parser}.TryParse(${self})";
        var result = CodeTemplate.From(canonical, CodeTemplateKind.Expression);
        Assert.NotNull(result);
        Assert.Equal(canonical, result!.Text);
    }

    [Fact]
    public void FromPreservesKind()
    {
        var result = CodeTemplate.From("${value}", CodeTemplateKind.TypeName);
        Assert.NotNull(result);
        Assert.Equal(CodeTemplateKind.TypeName, result!.Kind);
    }

    [Fact]
    public void FromNormalizesAllLegacyPlaceholdersTogether()
    {
        const string legacy = "$_parser.Foo($_self, $_syntax, $0)";
        var result = CodeTemplate.From(legacy, CodeTemplateKind.Expression, new Dictionary<string, string>(StringComparer.Ordinal) { ["0"] = "value" });
        Assert.NotNull(result);
        Assert.Equal("${parser}.Foo(${self}, ${syntax}, ${value})", result!.Text);
    }

    // -----------------------------------------------------------------------
    // CodeTemplateKind round-trips
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(CodeTemplateKind.Expression)]
    [InlineData(CodeTemplateKind.Statement)]
    [InlineData(CodeTemplateKind.StatementBlock)]
    [InlineData(CodeTemplateKind.TypeName)]
    public void KindIsPreserved(CodeTemplateKind kind)
    {
        var template = new CodeTemplate("something", kind);
        Assert.Equal(kind, template.Kind);
    }
}
