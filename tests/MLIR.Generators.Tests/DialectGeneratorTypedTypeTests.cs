namespace MLIR.Generators.Tests;

using Xunit;

public sealed class DialectGeneratorTypedTypeTests : DialectGeneratorTestBase
{
    [Fact]
    public void TypeDefWithParametersAndAssemblyFormatGeneratesTypedSyntaxAndReferenceClasses()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_OpaqueType : MyDialect_Type<\"opaque\"> {",
                "  let summary = \"an opaque type\";",
                "  let description = [{A type with a custom printed form.}];",
                "  let parameters = (ins StringRefParameter<\"the value\">:$value);",
                "  let assemblyFormat = \"`<` $value `>`\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "new TypeDefinition(\"myp.opaque\",",
            "factory: static context => new",
            "TypeSyntax? syntax = null)",
            ": base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)",
            "TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)",
            "WritePrefix(writer);",
            "new DialectTypePrefix(bangToken, nameToken)");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute",
            "DialectPrefixedAttributeValueSyntax");
    }

    [Fact]
    public void TypeDefWithParametersAndNoAssemblyFormatGeneratesTypedReferenceClassWithoutFactory()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
            "  let typeName = \"myp.\" # name;",
            "};",
            string.Empty,
            "def MyDialect_OpaqueType : MyDialect_Type<\"opaque\"> {",
            "  let csharpName = \"TypeDefinition.Name\";",
            "  let parameters = (ins StringRefParameter<\"the value\">:$value);",
            "};",
        ]);

        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_OpaqueType : MyDialect_Type<\"opaque\"> {",
                "  let csharpName = \"TypeDefinition.Name\";",
                "  let parameters = (ins StringRefParameter<\"the value\">:$value);",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "new TypeDefinition(\"myp.opaque\");",
            "TypeSyntax? syntax = null",
            ": base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)",
            "public string Value { get; }");

        AssertDoesNotContainAny(
            registrationSource,
            "factory: static context => new",
            "BindValueParam(",
            "OpaqueTypeSyntax",
            "OpaqueTypeAssemblyFormat");
    }
}
