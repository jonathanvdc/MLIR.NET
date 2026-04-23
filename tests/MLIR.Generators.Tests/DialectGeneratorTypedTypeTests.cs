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
            ": base(syntax)",
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
            ": base(syntax)",
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

    [Fact]
    public void GeneratedBuiltinAggregateTypesOwnTypeDefinitionsAndSemanticPayload()
    {
        var builtinSource = GenerateRegistrationSource(
            "builtin.td",
            "BuiltinDialectRegistration.g.cs",
            "include \"mlir/IR/BuiltinTypes.td\"");

        AssertContainsAll(
            builtinSource,
            "public sealed partial class FunctionType : TypeReference",
            "new TypeDefinition(\"builtin.function\", new global::MLIR.Dialects.Builtin.BuiltinFunctionTypeAssemblyFormat())",
            "public FunctionType(global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> inputs, global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> results, TypeSyntax? syntax = null)",
            "public global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> Inputs { get; }",
            "public global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> Results { get; }",
            "public sealed partial class TupleType : TypeReference",
            "new TypeDefinition(\"builtin.tuple\", new global::MLIR.Dialects.Builtin.BuiltinTupleTypeAssemblyFormat())",
            "public TupleType(global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> types, TypeSyntax? syntax = null)",
            "public global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference> Types { get; }",
            "dialect.AddType(FunctionType.TypeDefinition);",
            "dialect.AddType(TupleType.TypeDefinition);");
    }

    // ---------------------------------------------------------------------------
    // Issue #153: Generate C# marker interfaces for TypeInterface records
    // ---------------------------------------------------------------------------

    [Fact]
    public void MethodlessTypeInterfaceGeneratesPartialCSharpMarkerInterface()
    {
        // A TypeInterface with no methods should generate a partial C# marker interface.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_MarkerIface : TypeInterface<\"MyMarkerIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_FooType : MyDialect_Type<\"foo\", [MyDialect_MarkerIface]>;",
            ]);

        // Marker interface declaration should be emitted.
        AssertContainsAll(source, "public partial interface IMyMarkerIface", "{", "}");
        // Type class should implement it.
        AssertContainsAll(source, "public sealed partial class FooType : TypeReference, MLIR.Dialects.Mydialect.IMyMarkerIface");
        // No generated members inside the marker interface.
        AssertDoesNotContainAny(source, "IMyMarkerIface\r\n{\r\n    ");
    }

    [Fact]
    public void MethodBearingTypeInterfaceGeneratesMarkerInterfaceWithNoMembers()
    {
        // A TypeInterface that has method declarations should still generate a marker interface
        // with no C# members. Methods are out of scope for this layer.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_MethodIface : TypeInterface<\"MyMethodIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "  let methods = [",
                "    InterfaceMethod<\"get rank\", \"int64_t\", \"getRank\">,",
                "  ];",
                "};",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_RankedType : MyDialect_Type<\"ranked\", [MyDialect_MethodIface]>;",
            ]);

        // Marker interface is emitted with no C# method members.
        AssertContainsAll(source, "public partial interface IMyMethodIface");
        // Type class implements the marker interface.
        AssertContainsAll(source, "public sealed partial class RankedType : TypeReference, MLIR.Dialects.Mydialect.IMyMethodIface");
        // No C# method translation inside the interface.
        AssertDoesNotContainAny(source, "int64_t", "getRank");
    }

    [Fact]
    public void TypeDefWithMultipleTypeInterfacesEmitsAllInDeterministicOrder()
    {
        // Multiple type interfaces on one type should all be emitted in the same order as
        // they appear in the trait list.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_IfaceA : TypeInterface<\"MyIfaceA\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "def MyDialect_IfaceB : TypeInterface<\"MyIfaceB\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_MultiType : MyDialect_Type<\"multi\", [MyDialect_IfaceA, MyDialect_IfaceB]>;",
            ]);

        // Both marker interfaces are emitted.
        AssertContainsAll(source, "public partial interface IMyIfaceA", "public partial interface IMyIfaceB");
        // Type class implements both in trait-list order.
        AssertContainsAll(source,
            "public sealed partial class MultiType : TypeReference, MLIR.Dialects.Mydialect.IMyIfaceA, MLIR.Dialects.Mydialect.IMyIfaceB");
    }

    [Fact]
    public void TypeDefWithNoTypeInterfaceHasNoMarkerInterfaces()
    {
        // A type that has no type-interface traits should keep the plain TypeReference base class.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_PlainType2 : MyDialect_Type<\"plain2\">;",
            ]);

        // The class should have exactly the plain TypeReference base – no extra interfaces.
        AssertContainsAll(source, "public sealed partial class Plain2Type : TypeReference");
        AssertDoesNotContainAny(source, "Plain2Type : TypeReference,");
    }

    [Fact]
    public void InterfaceGenerationIsDrivenByMetadataNotHardCoding()
    {
        // Verify that the generation works for a completely custom (non-MLIR-builtin) interface
        // whose name does not appear in the generator source code. This demonstrates that
        // generation is purely metadata-driven.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_SomeNonStandardIface : TypeInterface<\"SomeNonStandardIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_SpecialType : MyDialect_Type<\"special\", [MyDialect_SomeNonStandardIface]>;",
            ]);

        AssertContainsAll(source, "public partial interface ISomeNonStandardIface");
        AssertContainsAll(source, "public sealed partial class SpecialType : TypeReference, MLIR.Dialects.Mydialect.ISomeNonStandardIface");
    }

    [Fact]
    public void InterfaceSuffixIsStrippedFromMarkerInterfaceName()
    {
        // Interface names ending with "Interface" should have the suffix stripped.
        // e.g., VectorElementTypeInterface -> IVectorElementType
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_VecElemIface : TypeInterface<\"VectorElementTypeInterface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name> : TypeDef<MyDialect_Dialect, name> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_VecType : MyDialect_Type<\"vec\", [MyDialect_VecElemIface]>;",
            ]);

        // "Interface" suffix is stripped → "IVectorElementType"
        AssertContainsAll(source, "public partial interface IVectorElementType");
        AssertContainsAll(source, "public sealed partial class VecType : TypeReference, MLIR.Dialects.Mydialect.IVectorElementType");
        // The original name without stripping should NOT appear as an interface.
        AssertDoesNotContainAny(source, "IVectorElementTypeInterface");
    }
}
