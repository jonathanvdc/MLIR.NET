namespace MLIR.Generators.Tests;

using System.Linq;
using Xunit;

public sealed class DialectGeneratorTypedTypeTests : DialectGeneratorTestBase
{
    [Fact]
    public void TypeAssemblyFormatRewriteUsesDeclaredPlainValueSyntaxShape()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "class PlainStringParam<string desc> : StringRefParameter<desc>;",
                "extends PlainStringParam : CSharpParameterExtension {",
                "  let csharpType = \"string\";",
                "  let csharpSyntaxType = \"StringAttributeValueSyntax\";",
                "  let csharpSyntaxShape = \"PlainValue\";",
                "  let csharpParser = \"$_parser.TryParseStringLiteralSyntax()\";",
                "  let csharpExtractor = \"$_syntax.Value\";",
                "  let csharpDefault = \"string.Empty\";",
                "  let csharpPrinter = \"new StringAttributeValueSyntax(TokenFactory.StringLiteral(StringLiteralAttributeAssemblyFormat.Quote($_self)), $_self)\";",
                "}",
                string.Empty,
                "def MyDialect_LabelType : MyDialect_Type<\"label\"> {",
                "  let parameters = (ins PlainStringParam<\"label\">:$value);",
                "  let assemblyFormat = \"`<` $value `>`\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public StringAttributeValueSyntax ValueSyntax { get; }",
            "rewriter.VisitToken(Literal0Token), ValueSyntax");
        AssertDoesNotContainAny(
            registrationSource,
            "(StringAttributeValueSyntax)rewriter.Visit(ValueSyntax)");
    }

    [Fact]
    public void TypeDefWithParametersAndAssemblyFormatGeneratesTypedSyntaxAndReferenceClasses()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
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
            "override TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)",
            "WritePrefix(writer);",
            ": BodyOnlyTypeAssemblyFormat",
            "ParseResult<TypeSyntax> TryParseBody(ParsingContext context, DialectTypePrefix prefix)",
            "ParseResult<TypeSyntax>.Success(new");
        AssertDoesNotContainAny(
            registrationSource,
            "factory:",
            "public NamedAttribute",
            "DialectPrefixedAttributeValueSyntax");
    }

    [Fact]
    public void TypeDefWithUnsupportedAssemblyFormatEmitsFastFailParserAndBindBuildMembers()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_UnsupportedType : MyDialect_Type<\"unsupported\"> {",
                "  let parameters = (ins StringRefParameter<\"the value\">:$value);",
                "  let assemblyFormat = \"`<` $value (`debug`)? `>`\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            ": DialectNamedTypeSyntax",
            "public StringAttributeValueSyntax ValueSyntax { get; }",
            ": BodyOnlyTypeAssemblyFormat",
            "Unsupported declarative assembly format construct for type body.",
            "public static TypeReference BindValue(TypeSyntax syntax, Binder binder)",
            "public override TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)",
            "StringLiteralAttributeAssemblyFormat.Quote(typed.Value)");
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
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
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
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
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
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
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
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
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
        var result = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            ensureUpstreamPrelude: true,
            ("builtin.td", "include \"mlir/IR/BuiltinTypes.td\""));
        Assert.Empty(result.Diagnostics);
        var builtinSource = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(static source => "// " + source.HintName + "\n" + source.SourceText));

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
        var result = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            ensureUpstreamPrelude: true,
            ("builtin.td", "include \"mlir/IR/BuiltinTypes.td\""));
        Assert.Empty(result.Diagnostics);
        var builtinSource = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(static source => "// " + source.HintName + "\n" + source.SourceText));

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
        var result = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            ensureUpstreamPrelude: true,
            ("builtin.td", "include \"mlir/IR/BuiltinTypes.td\""));
        Assert.Empty(result.Diagnostics);
        var builtinSource = string.Join(
            "\n",
            result.Results.Single().GeneratedSources.Select(static source => "// " + source.HintName + "\n" + source.SourceText));

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
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_MarkerIface : TypeInterface<\"MyMarkerIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_FooType : MyDialect_Type<\"foo\", [MyDialect_MarkerIface]>;",
            ]);

        // Marker interface declaration should be emitted.
        AssertContainsAll(source, "public partial interface IMyMarkerIface", "{", "}");
        // Type class should implement it.
        AssertContainsAll(source, "public sealed partial class fooType : TypeReference, MLIR.Dialects.Mydialect.IMyMarkerIface");
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
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_RankedType : MyDialect_Type<\"ranked\", [MyDialect_MethodIface]>;",
            ]);

        // Marker interface is emitted with no C# method members.
        AssertContainsAll(source, "public partial interface IMyMethodIface");
        // Type class implements the marker interface.
        AssertContainsAll(source, "public sealed partial class rankedType : TypeReference, MLIR.Dialects.Mydialect.IMyMethodIface");
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
                "include \"mlir/IR/Interfaces.td\"",
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
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_MultiType : MyDialect_Type<\"multi\", [MyDialect_IfaceA, MyDialect_IfaceB]>;",
            ]);

        // Both marker interfaces are emitted.
        AssertContainsAll(source, "public partial interface IMyIfaceA", "public partial interface IMyIfaceB");
        // Type class implements both in trait-list order.
        AssertContainsAll(source,
            "public sealed partial class multiType : TypeReference, MLIR.Dialects.Mydialect.IMyIfaceA, MLIR.Dialects.Mydialect.IMyIfaceB");
    }

    [Fact]
    public void TypeInterfaceProjectionEmitsXmlDocsFromUpstreamInterfaceAndMethodDescriptions()
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("shaped-docs.td", ComposeSource(
            [
                "include \"mlir/IR/BuiltinTypeInterfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def ShapedDocs_Dialect : Dialect {",
                "  let name = \"shapeddocs\";",
                "  let cppNamespace = \"::mlir::shapeddocs\";",
                "};",
                string.Empty,
                "class ShapedDocs_Type<string name, list<Trait> traits = []> : TypeDef<ShapedDocs_Dialect, name, traits> {",
                "  let typeName = \"shapeddocs.\" # name;",
                "};",
                string.Empty,
                "def ShapedDocs_Shaped : ShapedDocs_Type<\"shaped\", [ShapedTypeInterface]> {",
                "  let parameters = (ins \"ArrayRef<int64_t>\":$shape, \"Type\":$elementType);",
                "};",
                string.Empty,
                "extends ShapedDocs_Shaped : CSharpAttrOrTypeDefExtension {",
                "  let csharpParameters = (ins",
                "    \"global::System.Collections.Generic.IReadOnlyList<long>\":$shape,",
                "    \"global::MLIR.Semantics.TypeReference\":$elementType",
                "  );",
                "  let csharpInterfaceImplementations = [",
                "    CSharpInterfacePropertyImplementation<ShapedTypeInterface, \"HasRank\", \"true\">",
                "  ];",
                "};",
            ])));
        var source = string.Join("\n", generatedSources.Select(static result => result.SourceText.ToString()));

        AssertContainsAll(
            source,
            "/// <remarks>",
            "/// This interface provides a common API for interacting with multi-dimensional",
            "public partial interface IShapedType",
            "/// Returns the element type of this shaped type.",
            "global::MLIR.Semantics.TypeReference ElementType { get; }",
            "/// Returns if this type is ranked, i.e. it has a known number of dimensions.",
            "bool HasRank { get; }",
            "/// Returns the shape of this type if it is ranked, otherwise asserts.",
            "global::System.Collections.Generic.IReadOnlyList<long> Shape { get; }",
            "public sealed partial class shapedType : TypeReference, MLIR.Dialects.Prelude.IShapedType",
            "public global::MLIR.Semantics.TypeReference ElementType { get; }",
            "public bool HasRank => true;");
    }

    [Fact]
    public void TypeInterfaceProjectionOverlayDocsOverrideGeneratedDefaults()
    {
        var result = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            ensureUpstreamPrelude: true,
            ("documented-iface.td", ComposeSource(
            [
                "include \"mlir/Extensions/IR/CSharpExtensions.td\"",
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_Dialect : Dialect {",
                "  let name = \"mydialect\";",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "def MyDialect_DocumentedIface : TypeInterface<\"DocumentedIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "  let description = [{Upstream interface remarks.}];",
                "  let methods = [",
                "    InterfaceMethod<[{" ,
                "      Upstream method remarks.",
                "    }], \"int\", \"getValue\">,",
                "  ];",
                "};",
                string.Empty,
                "extends MyDialect_DocumentedIface : CSharpInterfaceExtension {",
                "  let csharpSummary = \"Projected interface summary.\";",
                "  let csharpDescription = [{Projected interface remarks.}];",
                "  let csharpMembers = [",
                "    CSharpInterfaceProperty<\"getValue\", \"int\", \"Value\"> {",
                "      let csharpSummary = \"Projected property summary.\";",
                "      let csharpDescription = [{Projected property remarks.}];",
                "    }",
                "  ];",
                "};",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_DocType : MyDialect_Type<\"doc\", [MyDialect_DocumentedIface]> {",
                "  let parameters = (ins \"int\":$value);",
                "};",
                string.Empty,
                "extends MyDialect_DocType : CSharpAttrOrTypeDefExtension {",
                "  let csharpParameters = (ins \"int\":$value);",
                "};",
            ])));
        Assert.Empty(result.Diagnostics);
        var source = string.Join("\n", result.Results.Single().GeneratedSources.Select(static generated => generated.SourceText.ToString()));

        AssertContainsAll(
            source,
            "/// <summary>Projected interface summary.</summary>",
            "/// Projected interface remarks.",
            "public partial interface IDocumentedIface",
            "/// <summary>Projected property summary.</summary>",
            "/// Projected property remarks.",
            "int Value { get; }",
            "public sealed partial class docType : TypeReference, MLIR.Dialects.Mydialect.IDocumentedIface",
            "public int Value { get; }");
        AssertDoesNotContainAny(
            source,
            "Upstream interface remarks.",
            "Upstream method remarks.");
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
        AssertContainsAll(source, "public sealed partial class plain2Type : TypeReference");
        AssertDoesNotContainAny(source, "plain2Type : TypeReference,");
    }

    [Fact]
    public void InterfaceGenerationIsDrivenByMetadataNotHardCoding()
    {
        // Verify that the generation works for a completely custom (non-MLIR-builtin) interface
        // whose name does not appear in the generator source code. This demonstrates that
        // generation is purely metadata-driven.
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_SomeNonStandardIface : TypeInterface<\"SomeNonStandardIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_SpecialType : MyDialect_Type<\"special\", [MyDialect_SomeNonStandardIface]>;",
            ]);

        AssertContainsAll(source, "public partial interface ISomeNonStandardIface");
        AssertContainsAll(source, "public sealed partial class specialType : TypeReference, MLIR.Dialects.Mydialect.ISomeNonStandardIface");
    }

    [Fact]
    public void InterfaceSuffixIsStrippedFromMarkerInterfaceName()
    {
        // Interface names ending with "Interface" should have the suffix stripped.
        // e.g., VectorElementTypeInterface -> IVectorElementType
        var source = GenerateMyDialectRegistrationSource(
            [
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_VecElemIface : TypeInterface<\"VectorElementTypeInterface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_VecType : MyDialect_Type<\"vec\", [MyDialect_VecElemIface]>;",
            ]);

        // "Interface" suffix is stripped → "IVectorElementType"
        AssertContainsAll(source, "public partial interface IVectorElementType");
        AssertContainsAll(source, "public sealed partial class vecType : TypeReference, MLIR.Dialects.Mydialect.IVectorElementType");
        // The original name without stripping should NOT appear as an interface.
        AssertDoesNotContainAny(source, "IVectorElementTypeInterface");
    }

    [Fact]
    public void TypeInterfaceOverlayEmitsMemberBearingInterfaceAndTypeImplementations()
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("shaped.td", ComposeSource(
            [
                "include \"mlir/IR/BuiltinTypeInterfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def ShapedTest_Dialect : Dialect {",
                "  let name = \"shapedtest\";",
                "  let cppNamespace = \"::mlir::shapedtest\";",
                "};",
                string.Empty,
                "class ShapedTest_Type<string name, list<Trait> traits = []> : TypeDef<ShapedTest_Dialect, name, traits> {",
                "  let typeName = \"shapedtest.\" # name;",
                "};",
                string.Empty,
                "def ShapedTest_Shaped : ShapedTest_Type<\"shaped\", [ShapedTypeInterface]> {",
                "  let parameters = (ins \"ArrayRef<int64_t>\":$shape, \"Type\":$elementType);",
                "};",
                string.Empty,
                "extends ShapedTest_Shaped : CSharpAttrOrTypeDefExtension {",
                "  let csharpParameters = (ins",
                "    \"global::System.Collections.Generic.IReadOnlyList<long>\":$shape,",
                "    \"global::MLIR.Semantics.TypeReference\":$elementType",
                "  );",
                "  let csharpInterfaceImplementations = [",
                "    CSharpInterfacePropertyImplementation<ShapedTypeInterface, \"HasRank\", \"true\">",
                "  ];",
                "};",
            ])));
        var source = string.Join("\n", generatedSources.Select(static result => result.SourceText.ToString()));

        AssertContainsAll(
            source,
            "public partial interface IShapedType",
            "global::MLIR.Semantics.TypeReference ElementType { get; }",
            "bool HasRank { get; }",
            "global::System.Collections.Generic.IReadOnlyList<long> Shape { get; }",
            "public sealed partial class shapedType : TypeReference, MLIR.Dialects.Prelude.IShapedType",
            "public global::System.Collections.Generic.IReadOnlyList<long> Shape { get; }",
            "public global::MLIR.Semantics.TypeReference ElementType { get; }",
            "public bool HasRank => true;");
    }

    [Fact]
    public void MissingMappedInterfaceImplementationProducesClearDiagnostic()
    {
        var result = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            ensureUpstreamPrelude: true,
            ("missing-shape.td", ComposeSource(
            [
                "include \"mlir/IR/BuiltinTypeInterfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                "def MissingShape_Dialect : Dialect {",
                "  let name = \"missingshape\";",
                "  let cppNamespace = \"::mlir::missingshape\";",
                "};",
                string.Empty,
                "class MissingShape_Type<string name, list<Trait> traits = []> : TypeDef<MissingShape_Dialect, name, traits> {",
                "  let typeName = \"missingshape.\" # name;",
                "};",
                string.Empty,
                "def MissingShape_Foo : MissingShape_Type<\"foo\", [ShapedTypeInterface]> {",
                "  let parameters = (ins \"Type\":$elementType);",
                "};",
                string.Empty,
                "extends MissingShape_Foo : CSharpAttrOrTypeDefExtension {",
                "  let csharpParameters = (ins \"global::MLIR.Semantics.TypeReference\":$elementType);",
                "};",
            ])));

        var diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "MLIRGEN003"));
        Assert.Contains("type", diagnostic.GetMessage());
        Assert.Contains("MissingShape_Foo", diagnostic.GetMessage());
        Assert.Contains("missingshape", diagnostic.GetMessage());
        Assert.Contains("ShapedTypeInterface", diagnostic.GetMessage());
        Assert.Contains("HasRank", diagnostic.GetMessage());
    }
}
