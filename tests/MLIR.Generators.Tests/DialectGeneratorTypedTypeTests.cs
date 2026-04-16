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
            "TypeSyntax? syntax = null)",
            ": base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)",
            "TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)",
            "WritePrefix(writer);",
            "new DialectTypePrefix(bangToken, nameToken)");
        AssertDoesNotContainAny(
            registrationSource,
            "factory:",
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

    [Fact]
    public void PlainTypeDefWithNoParametersGeneratesNoFactoryDelegate()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_PlainType : MyDialect_Type<\"plain\"> {",
                "  let summary = \"a plain type with no parameters\";",
                "};",
            ]);

        // Plain types without parameters must not emit a factory delegate. Binding falls
        // back to UnknownTypeReference when no assembly format is registered.
        AssertContainsAll(
            registrationSource,
            "new TypeDefinition(\"myp.plain\");",
            "TypeSyntax? syntax = null)");

        AssertDoesNotContainAny(
            registrationSource,
            "factory:",
            "BindValue(TypeReferenceConstructionContext");
    }

    [Fact]
    public void PlainTypeDefWithCsharpAssemblyFormatPassesItToTypeDefinition()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_SentinelType : MyDialect_Type<\"sentinel\"> {",
                "  let summary = \"a sentinel type with a custom assembly format\";",
                "  let csharpAssemblyFormat = \"new global::MyNs.MySentinelAssemblyFormat()\";",
                "};",
            ]);

        // The csharpAssemblyFormat should be forwarded to the TypeDefinition constructor even
        // for zero-parameter plain types.
        AssertContainsAll(
            registrationSource,
            "new TypeDefinition(\"myp.sentinel\", new global::MyNs.MySentinelAssemblyFormat())",
            "TypeSyntax? syntax = null)");
    }

    [Fact]
    public void PlainTypeDefWithCsharpNameOverridesNameProperty()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_MnemonicType : MyDialect_Type<\"mnemonic\"> {",
                "  let summary = \"a type with an overridden short name\";",
                "  let csharpName = \"\\\"short\\\"\";",
                "};",
            ]);

        // csharpName should be used for the Name property override in the generated class.
        AssertContainsAll(
            registrationSource,
            "Name => \"short\"");
    }

    [Fact]
    public void GeneratedBuiltinFloatTypesDoNotInheritFromFloatTypeReference()
    {
        var builtinSource = GenerateRegistrationSource(
            "builtin.td",
            "BuiltinDialectRegistration.g.cs",
            "include \"mlir/IR/BuiltinTypes.td\"");

        // Float types must be plain TypeReference subclasses, not FloatTypeReference subclasses.
        AssertContainsAll(
            builtinSource,
            "public sealed partial class Float32Type : TypeReference",
            "public sealed partial class Float16Type : TypeReference",
            "public sealed partial class BFloat16Type : TypeReference",
            "public sealed partial class Float64Type : TypeReference",
            "public sealed partial class FloatTF32Type : TypeReference");

        AssertDoesNotContainAny(
            builtinSource,
            ": FloatTypeReference");
    }

    [Fact]
    public void GeneratedBuiltinFloatTypesCarryMnemonicInNameProperty()
    {
        var builtinSource = GenerateRegistrationSource(
            "builtin.td",
            "BuiltinDialectRegistration.g.cs",
            "include \"mlir/IR/BuiltinTypes.td\"");

        // Name property must return the MLIR mnemonic, not the qualified registry key.
        AssertContainsAll(
            builtinSource,
            "Name => \"f32\"",
            "Name => \"f16\"",
            "Name => \"bf16\"",
            "Name => \"f64\"",
            "Name => \"tf32\"");
    }

    [Fact]
    public void GeneratedBuiltinIndexAndNoneUseNormalTypeEmissionPath()
    {
        var builtinSource = GenerateRegistrationSource(
            "builtin.td",
            "BuiltinDialectRegistration.g.cs",
            "include \"mlir/IR/BuiltinTypes.td\"");

        // IndexType and NoneType are fully generated via the standard path with
        // csharpAssemblyFormat and csharpName.
        AssertContainsAll(
            builtinSource,
            "new TypeDefinition(\"builtin.index\", new global::MLIR.Dialects.Builtin.BuiltinIndexTypeAssemblyFormat())",
            "new TypeDefinition(\"builtin.none\", new global::MLIR.Dialects.Builtin.BuiltinNoneTypeAssemblyFormat())",
            "Name => \"index\"",
            "Name => \"none\"",
            "public sealed partial class IndexType : TypeReference",
            "public sealed partial class NoneType : TypeReference");
    }
}
