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
            "public sealed class opaqueTypeSyntax : DialectNamedTypeSyntax",
            "public opaqueTypeSyntax(DialectTypePrefix prefix",
            "public StringAttributeValueSyntax ValueSyntax { get; }",
            "public sealed class opaqueType : TypeReference",
            "public string Value { get; }",
            "public opaqueType(string value)",
            "public opaqueType(TypeReferenceConstructionContext context)",
            "internal sealed class opaqueTypeAssemblyFormat : ITypeAssemblyFormat",
            "ParseResult<TypeSyntax> TryParse(TypeParsingContext context)",
            "TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)",
            "TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)",
            "WritePrefix(writer);",
            "new DialectTypePrefix(bangToken, nameToken)",
            "new TypeDefinition(\"myp.opaque\", new opaqueTypeAssemblyFormat()");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute",
            "DialectPrefixedAttributeValueSyntax");
    }
}
