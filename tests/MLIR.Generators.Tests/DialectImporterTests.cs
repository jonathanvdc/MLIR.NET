namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.ODS;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;
using TableGen;
using Xunit;

public sealed class DialectImporterTests
{
    [Fact]
    public void ImportsActualOdsStyleDialectOperationAttributeTypeAndConstraintRecords()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "class MiniArith_Attr<string name> : AttrDef<MiniArith_Dialect, name> {\n" +
            "  let mnemonic = name;\n" +
            "  let attrName = \"miniarith.\" # name;\n" +
            "};\n" +
            "class MiniArith_Type<string name> : TypeDef<MiniArith_Dialect, name> {\n" +
            "  let mnemonic = name;\n" +
            "  let typeName = \"miniarith.\" # name;\n" +
            "};\n" +
            "class MiniArith_I32Constraint : TypedSignlessIntegerAttrBase<I32, \"uint32_t\", \"32-bit signless integer attribute\">;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "  let summary = \"Arithmetic dialect\";\n" +
            "  let description = [{This dialect defines basic integer and floating point arithmetic ops.}];\n" +
            "  let hasConstantMaterializer = 1;\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let description = [{Produces a constant integer value.}];\n" +
            "  let arguments = (ins I32Attr:$value, BoolAttr:$enabled);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value $enabled attr-dict\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_I32Attr : MiniArith_Attr<\"i32\">;\n" +
            "def MiniArith_I32Type : MiniArith_Type<\"i32\">;\n" +
            "def MiniArith_I32ConstraintAttr : MiniArith_I32Constraint;\n" +
            "\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        Assert.Equal(2, dialects.Count);
        var prelude = dialects[0];
        var dialect = dialects[1];
        var constantOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.constant");
        var addiOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.addi");
        var attribute = Assert.Single(dialect.Attributes, static attr => attr.Name == "miniarith.i32");
        var type = Assert.Single(dialect.Types, static typeModel => typeModel.Name == "miniarith.i32");
        var constraint = Assert.Single(prelude.AttributeConstraints, static attrConstraint => attrConstraint.Name == "MiniArith_I32ConstraintAttr");

        Assert.Equal("miniarith", dialect.Name);
        Assert.Equal("::mlir::miniarith", dialect.CppNamespace);
        Assert.Equal("Arithmetic dialect", dialect.Summary);
        Assert.Contains("basic integer and floating point arithmetic ops", dialect.Description);
        Assert.True(dialect.HasConstantMaterializer);
        Assert.Equal(AttributeConstraintKind.IntegerLiteral, constraint.Kind);

        Assert.Equal("MiniArith_ConstantOp", constantOp.ClassName);
        Assert.Equal(["result"], constantOp.Results.Select(static result => result.Name).ToArray());
        Assert.Equal(["value", "enabled"], constantOp.Attributes.Select(static attributeUse => attributeUse.Name).ToArray());
        Assert.Equal("I32Attr", constantOp.Attributes[0].ConstraintRecordName);
        Assert.Equal("BoolAttr", constantOp.Attributes[1].ConstraintRecordName);
        Assert.Empty(constantOp.Operands);
        Assert.Equal(["Pure"], constantOp.Traits.Select(static t => t.RecordName).ToArray());
        Assert.NotNull(constantOp.AssemblyFormat);
        Assert.Collection(
            constantOp.AssemblyFormat!.Elements,
            e => Assert.IsType<VariableChunk>(e),
            e => Assert.IsType<VariableChunk>(e),
            e => Assert.IsType<AttrDictDirectiveChunk>(e));
        Assert.Equal("integer constant", constantOp.Summary);
        Assert.Contains("Produces a constant integer value", constantOp.Description);

        Assert.Equal("MiniArith_AddIOp", addiOp.ClassName);
        Assert.Equal(["lhs", "rhs"], addiOp.Operands.Select(static operand => operand.Name).ToArray());
        Assert.Equal(["result"], addiOp.Results.Select(static result => result.Name).ToArray());
        Assert.Empty(addiOp.Attributes);
        Assert.Equal(["Pure", "Commutative"], addiOp.Traits.Select(static t => t.RecordName).ToArray());
        // Commutative is a NativeTrait (NativeOpTrait) with C++ trait info.
        var commutativeTrait = Assert.IsType<NativeTraitModel>(addiOp.Traits[1]);
        Assert.Equal("IsCommutative", commutativeTrait.Trait);
        Assert.Equal("::mlir::OpTrait", commutativeTrait.CppNamespace);
        Assert.NotNull(addiOp.AssemblyFormat);
        Assert.Collection(
            addiOp.AssemblyFormat!.Elements,
            e => Assert.Equal("lhs", Assert.IsType<VariableChunk>(e).Name),
            e => Assert.Equal(TokenKind.Comma, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(e).Value)).TokenKind),
            e => Assert.Equal("rhs", Assert.IsType<VariableChunk>(e).Name),
            e => Assert.IsType<AttrDictDirectiveChunk>(e),
            e => Assert.Equal(TokenKind.Colon, Assert.IsType<PunctuationLiteral>(Assert.Single(Assert.IsType<LiteralChunk>(e).Value)).TokenKind),
            e => Assert.Equal("result", Assert.IsType<VariableOperand>(Assert.IsType<TypeDirectiveChunk>(e).Operand).Name));

        Assert.Equal("MiniArith_I32Attr", attribute.RecordName);
        Assert.Equal("MiniArith_I32Type", type.RecordName);
    }

    [Fact]
    public void ImportsNativeTraitListAndGenInternalTraitModels()
    {
        // Verify that the importer produces the correct TraitModel subclass for each
        // trait category: NativeTrait (and NativeOpTrait), TraitList, GenInternalTrait,
        // and plain Trait (SimpleTraitModel).
        const string source =
            "class MyTest_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyTest_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyTest_Dialect : Dialect {\n" +
            "  let name = \"mytest\";\n" +
            "};\n" +
            "\n" +
            // A plain Trait subclass with no further classification.
            "class SimpleTrait : Trait;\n" +
            "def MySimpleTrait : SimpleTrait;\n" +
            "\n" +
            // A NativeOpTrait (which extends NativeTrait) - should produce NativeTraitModel.
            "def MyNativeTrait : NativeOpTrait<\"MyNativeOp\">;\n" +
            "\n" +
            // A TraitList - should produce TraitListModel with constituent traits.
            "def MyTraitList : TraitList<[MyNativeTrait, MySimpleTrait]>;\n" +
            "\n" +
            // A GenInternalTrait - should produce GenInternalTraitModel.
            "def MyGenInternal : GenInternalTrait<\"MyInternal\", \"Op\">;\n" +
            "\n" +
            "def MyTest_AllTraitsOp : MyTest_Op<\"alltraits\",\n" +
            "    [MySimpleTrait, MyNativeTrait, MyTraitList, MyGenInternal]>;\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "mytest");
        var op = Assert.Single(dialect.Operations);

        Assert.Equal(4, op.Traits.Count);
        Assert.Equal(
            ["MySimpleTrait", "MyNativeTrait", "MyTraitList", "MyGenInternal"],
            op.Traits.Select(static t => t.RecordName).ToArray());

        // Plain Trait subclass → SimpleTraitModel.
        Assert.IsType<SimpleTraitModel>(op.Traits[0]);

        // NativeOpTrait → NativeTraitModel with trait and cppNamespace.
        var nativeTrait = Assert.IsType<NativeTraitModel>(op.Traits[1]);
        Assert.Equal("MyNativeOp", nativeTrait.Trait);
        Assert.Equal("::mlir::OpTrait", nativeTrait.CppNamespace);

        // TraitList → TraitListModel with constituent trait models.
        var traitList = Assert.IsType<TraitListModel>(op.Traits[2]);
        Assert.Equal(2, traitList.Traits.Count);
        Assert.IsType<NativeTraitModel>(traitList.Traits[0]);
        Assert.IsType<SimpleTraitModel>(traitList.Traits[1]);

        // GenInternalTrait → GenInternalTraitModel with the trait identifier.
        var genInternal = Assert.IsType<GenInternalTraitModel>(op.Traits[3]);
        Assert.Equal("::mlir::OpTrait::MyInternal", genInternal.Trait);
    }

    [Fact]
    public void TreatsEmptyStringFieldsAsAbsent()
    {
        // When a base class supplies empty-string defaults (e.g. from an ODS prelude),
        // GetOptionalStringField must return null so callers fall back correctly.
        const string source =
            "class MyDialect_Op<string mnemonic> : Op<MyDialect_Dialect, mnemonic, []> {\n" +
            "  string cppClassName = \"\";\n" +   // explicit empty default
            "  string summary = \"\";\n" +
            "  string assemblyFormat = \"\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_FooOp : MyDialect_Op<\"foo\">;\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        Assert.Equal(2, dialects.Count);
        var dialect = dialects[1];
        var op = Assert.Single(dialect.Operations);

        // cppClassName="" must fall back to the record name, not produce an empty class name.
        Assert.Equal("MyDialect_FooOp", op.ClassName);
        // summary="" must be treated as absent (null).
        Assert.Null(op.Summary);
        // assemblyFormat="" must be treated as absent (no custom assembly format).
        Assert.Null(op.AssemblyFormat);
    }

    [Fact]
    public void ImportsEnumAndBitEnumModelsFromUpstreamStyleRecords()
    {
        const string source =
            "include \"mlir/IR/EnumAttr.td\"\n" +
            "\n" +
            "def MiniEnum_Dialect : Dialect {\n" +
            "  let name = \"minienum\";\n" +
            "  let cppNamespace = \"::mlir::minienum\";\n" +
            "};\n" +
            "\n" +
            "def MINI_MODE_A : I32EnumAttrCase<\"a\", 0>;\n" +
            "def MINI_MODE_B : I32EnumAttrCase<\"b\", 1>;\n" +
            "def MiniEnum_Mode : I32EnumAttr<\"Mode\", \"mode summary\", [MINI_MODE_A, MINI_MODE_B]> {\n" +
            "  let cppNamespace = \"::mlir::minienum\";\n" +
            "  let genSpecializedAttr = 0;\n" +
            "};\n" +
            "def MiniEnum_ModeAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Mode, \"mode\"> {\n" +
            "  let assemblyFormat = \"`<` $value `>`\";\n" +
            "};\n" +
            "\n" +
            "def MINI_FLAG_NONE : I32BitEnumAttrCaseNone<\"none\">;\n" +
            "def MINI_FLAG_X : I32BitEnumAttrCaseBit<\"x\", 0>;\n" +
            "def MINI_FLAG_Y : I32BitEnumAttrCaseBit<\"y\", 1>;\n" +
            "def MINI_FLAG_XY : I32BitEnumAttrCaseGroup<\"xy\", [MINI_FLAG_X, MINI_FLAG_Y]>;\n" +
            "def MiniEnum_Flags : I32BitEnumAttr<\"Flags\", \"flags summary\", [MINI_FLAG_NONE, MINI_FLAG_X, MINI_FLAG_Y, MINI_FLAG_XY]> {\n" +
            "  let separator = \",\";\n" +
            "  let cppNamespace = \"::mlir::minienum\";\n" +
            "  let genSpecializedAttr = 0;\n" +
            "  let printBitEnumPrimaryGroups = 1;\n" +
            "};\n" +
            "def MiniEnum_FlagsAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Flags, \"flags\"> {\n" +
            "  let assemblyFormat = \"`<` $value `>`\";\n" +
            "};";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        Assert.Equal(2, dialects.Count);
        var prelude = dialects[0];
        var dialect = dialects[1];
        var modeAttr = Assert.Single(dialect.Attributes, static attr => attr.RecordName == "MiniEnum_ModeAttr");
        var flagsAttr = Assert.Single(dialect.Attributes, static attr => attr.RecordName == "MiniEnum_FlagsAttr");

        // Enum constraints whose cppNamespace matches the dialect are routed to the dialect,
        // not the prelude.  Both MiniEnum_Mode and MiniEnum_Flags have cppNamespace =
        // "::mlir::minienum", so they belong to the minienum dialect.
        var modeConstraint = Assert.Single(dialect.AttributeConstraints, static attr => attr.RecordName == "MiniEnum_Mode");
        var flagsConstraint = Assert.Single(dialect.AttributeConstraints, static attr => attr.RecordName == "MiniEnum_Flags");
        Assert.DoesNotContain(prelude.AttributeConstraints, static attr =>
            attr.RecordName == "MiniEnum_Mode" || attr.RecordName == "MiniEnum_Flags");

        Assert.Equal("Mode", modeAttr.EnumModel!.ClassName);
        Assert.False(modeAttr.EnumModel.IsBitEnum);
        Assert.Equal(["a", "b"], modeAttr.EnumModel.Cases.Select(static c => c.Str).ToArray());

        Assert.Equal("Flags", flagsAttr.EnumModel!.ClassName);
        Assert.True(flagsAttr.EnumModel.IsBitEnum);
        Assert.Equal(",", flagsAttr.EnumModel.Separator);
        Assert.Equal(new long[] { 0, 1, 2, 3 }, flagsAttr.EnumModel.Cases.Select(static c => c.Value).ToArray());

        Assert.NotNull(modeConstraint.EnumModel);
        Assert.False(modeConstraint.EnumModel!.IsBitEnum);

        Assert.NotNull(flagsConstraint.EnumModel);
        Assert.True(flagsConstraint.EnumModel!.IsBitEnum);
    }

    [Fact]
    public void ImportsBuiltinIntegerTypeParameterCsharpMetadataFromPreludeOverlay()
    {
        const string source =
            "include \"mlir/IR/BuiltinTypes.td\"\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var builtin = Assert.Single(dialects, static d => d.Name == "builtin");
        var integer = Assert.Single(builtin.Types, static typeModel => typeModel.RecordName == "Builtin_Integer");

        Assert.Collection(
            integer.Parameters,
            width =>
            {
                Assert.Equal("width", width.Name);
                Assert.Equal("unsigned", width.CppType);
                Assert.Equal("int", width.CsharpType);
                Assert.Equal("$_syntax.Width", width.CsharpExtractor);
                Assert.Equal("0", width.CsharpDefault);
            },
            signedness =>
            {
                Assert.Equal("signedness", signedness.Name);
                Assert.Equal("SignednessSemantics", signedness.CppType);
                Assert.Equal("global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness", signedness.CsharpType);
                Assert.Equal("$_syntax.Signedness", signedness.CsharpExtractor);
                Assert.Equal("global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signless", signedness.CsharpDefault);
            });

        Assert.Equal("new global::MLIR.Dialects.Builtin.BuiltinIntegerTypeAssemblyFormat()", integer.CsharpAssemblyFormat);
    }

    [Fact]
    public void ImportsBuiltinTypeAndAttributeParameterCsharpMetadataFromPreludeOverlay()
    {
        const string source =
            "include \"mlir/IR/BuiltinAttributes.td\"\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var builtin = Assert.Single(dialects, static d => d.Name == "builtin");

        var typeAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_TypeAttr");
        var typeParam = Assert.Single(typeAttr.Parameters);
        Assert.Equal("value", typeParam.Name);
        Assert.Equal("global::MLIR.Semantics.TypeReference", typeParam.CsharpType);
        Assert.Equal("global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax", typeParam.CsharpSyntaxType);
        Assert.Equal("$_syntax.TypeSyntax", typeParam.CsharpExtractor);

        var arrayAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_ArrayAttr");
        var arrayParam = Assert.Single(arrayAttr.Parameters);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>", arrayParam.CsharpType);
        Assert.Equal("global::MLIR.Syntax.Attributes.Collections.ArrayAttributeValueSyntax", arrayParam.CsharpSyntaxType);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeItems($_syntax.Items.Items)", arrayParam.CsharpExtractor);
        Assert.Equal("global::System.Array.Empty<global::MLIR.Semantics.AttributeValue>()", arrayParam.CsharpDefault);

        var dictionaryAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_DictionaryAttr");
        var dictionaryParam = Assert.Single(dictionaryAttr.Parameters);
        Assert.Equal("global::MLIR.Semantics.NamedAttributeCollection", dictionaryParam.CsharpType);
        Assert.Equal("global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax", dictionaryParam.CsharpSyntaxType);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes($_syntax.Attributes.Items)", dictionaryParam.CsharpExtractor);
        Assert.Equal("global::MLIR.Semantics.NamedAttributeCollection.Empty", dictionaryParam.CsharpDefault);

        var integerAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_IntegerAttr");
        Assert.NotNull(integerAttr.SelfTypeParameter);
        Assert.True(integerAttr.SelfTypeParameter!.IsSelfTypeParameter);
        Assert.Equal("type", integerAttr.SelfTypeParameter.Name);
        Assert.Equal("global::MLIR.Semantics.TypeReference", integerAttr.SelfTypeParameter.CsharpType);
        Assert.Equal("global::MLIR.Semantics.TypeFactory.None", integerAttr.SelfTypeParameter.CsharpDefault);
        Assert.Single(integerAttr.PayloadParameters);
        Assert.Equal("value", integerAttr.PayloadParameters[0].Name);

        var stringAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_StringAttr");
        Assert.NotNull(stringAttr.SelfTypeParameter);
        Assert.True(stringAttr.SelfTypeParameter!.IsSelfTypeParameter);
        Assert.Equal("type", stringAttr.SelfTypeParameter.Name);
        Assert.Equal("global::MLIR.Semantics.TypeFactory.None", stringAttr.SelfTypeParameter.CsharpDefault);
        Assert.Single(stringAttr.PayloadParameters);
        Assert.Equal("value", stringAttr.PayloadParameters[0].Name);

        var floatAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_FloatAttr");
        Assert.NotNull(floatAttr.SelfTypeParameter);
        Assert.True(floatAttr.SelfTypeParameter!.IsSelfTypeParameter);
        Assert.Equal("type", floatAttr.SelfTypeParameter.Name);
        Assert.Equal("global::MLIR.Semantics.TypeFactory.None", floatAttr.SelfTypeParameter.CsharpDefault);
        Assert.Single(floatAttr.PayloadParameters);
        Assert.Equal("value", floatAttr.PayloadParameters[0].Name);

        var opaqueAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_OpaqueAttr");
        Assert.NotNull(opaqueAttr.SelfTypeParameter);
        Assert.True(opaqueAttr.SelfTypeParameter!.IsSelfTypeParameter);
        Assert.Equal("global::MLIR.Semantics.TypeFactory.None", opaqueAttr.SelfTypeParameter.CsharpDefault);
        Assert.Equal(2, opaqueAttr.PayloadParameters.Count);
        Assert.Equal(["dialectNamespace", "attrData"], opaqueAttr.PayloadParameters.Select(static param => param.Name).ToArray());
    }

    [Fact]
    public void ImportsSharedBuiltinTypeConstraintsWithCanonicalBuiltinNames()
    {
        var dialects = DialectImporter.Import(
            GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(
                "def MiniArith_Dialect : Dialect { let name = \"miniarith\"; let cppNamespace = \"::mlir::miniarith\"; }\n").Evaluate());

        Assert.Equal(2, dialects.Count);
        var prelude = dialects[0];
        var i32 = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "I32");
        var f32 = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "F32");
        var index = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "Index");
        var noneType = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "NoneType");
        var tuple = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "AnyTuple");
        var function = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "FunctionType");
        var tensor = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "AnyTensor");
        var vector = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "AnyVectorOfAnyRank");
        var memRef = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "AnyMemRef");
        var i32Tensor = Assert.Single(prelude.TypeConstraints, static constraint => constraint.RecordName == "I32Tensor");

        Assert.Equal(TypeConstraintKind.ExactInteger, i32.Kind);
        Assert.Equal("i32", i32.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.ExactFloat, f32.Kind);
        Assert.Equal("f32", f32.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.IndexType, index.Kind);
        Assert.Equal("index", index.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.NoneType, noneType.Kind);
        Assert.Equal("none", noneType.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.TupleType, tuple.Kind);
        Assert.Equal("builtin.tuple", tuple.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.FunctionType, function.Kind);
        Assert.Equal("builtin.function", function.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.TensorType, tensor.Kind);
        Assert.Equal("tensor", tensor.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.VectorType, vector.Kind);
        Assert.Equal("vector", vector.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.MemRefType, memRef.Kind);
        Assert.Equal("memref", memRef.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.None, i32Tensor.Kind);
        Assert.Null(i32Tensor.CanonicalTypeName);
    }

    [Fact]
    public void ImportsMlirNetAssemblyExtensionOverlayForUpstreamSelectOp()
    {
        var dialects = DialectImporter.Import(
            GeneratorTestHelpers.LoadTableGenFromPrelude("include \"mlir/Dialect/Arith/IR/ArithOps.td\"").Evaluate());

        var arith = Assert.Single(dialects, static dialect => dialect.Name == "arith");
        var select = Assert.Single(arith.Operations, static operation => operation.Name == "arith.select");

        Assert.Null(select.AssemblyFormat);
        Assert.Equal("global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance", select.AssemblyFormatCode);
    }

    [Fact]
    public void ImportsUnnamedVariadicResultsFromUpstreamFuncCall()
    {
        var dialects = DialectImporter.Import(
            GeneratorTestHelpers.LoadTableGenFromPrelude("include \"mlir/Dialect/Func/IR/FuncOps.td\"").Evaluate());

        var func = Assert.Single(dialects, static dialect => dialect.Name == "func");
        var call = Assert.Single(func.Operations, static operation => operation.Name == "func.call");
        var result = Assert.Single(call.Results);

        Assert.Equal("results", result.Name);
        Assert.True(result.IsVariadic);
    }

    [Fact]
    public void ImportsMlirNetAssemblyExtensionOverlayForUpstreamFuncOp()
    {
        var dialects = DialectImporter.Import(
            GeneratorTestHelpers.LoadTableGenFromPrelude("include \"mlir/Dialect/Func/IR/FuncOps.td\"").Evaluate());

        var func = Assert.Single(dialects, static dialect => dialect.Name == "func");
        var funcOp = Assert.Single(func.Operations, static operation => operation.Name == "func.func");

        Assert.Null(funcOp.AssemblyFormat);
        Assert.Equal("global::MLIR.Dialects.Extensions.FuncOperationAssemblyFormat.Instance", funcOp.AssemblyFormatCode);
    }

    [Fact]
    public void ImportsBuiltinModuleOpFromPrelude()
    {
        var dialects = DialectImporter.Import(
            GeneratorTestHelpers.LoadTableGenFromPrelude("include \"mlir/IR/BuiltinOps.td\"").Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "builtin");
        var moduleOp = Assert.Single(dialect.Operations, static op => op.Name == "builtin.module");

        Assert.NotNull(moduleOp.AssemblyFormat);
        Assert.Equal("builtin.module", moduleOp.Name);
    }

    [Fact]
    public void ImportsAttrDefParametersWithShorthandStringType()
    {
        // Inline C++ type strings in the parameters dag are the shorthand form.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_PairAttr : MyP_Attr<\"pair\"> {\n" +
            "  let parameters = (ins \"unsigned\":$first, \"unsigned\":$second);\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_PairAttr");

        Assert.Equal(2, attr.Parameters.Count);
        Assert.Equal("first", attr.Parameters[0].Name);
        Assert.Equal("unsigned", attr.Parameters[0].CppType);
        Assert.Null(attr.Parameters[0].ConstraintRecordName);
        Assert.Null(attr.Parameters[0].CsharpType);

        Assert.Equal("second", attr.Parameters[1].Name);
        Assert.Equal("unsigned", attr.Parameters[1].CppType);
        Assert.Null(attr.Parameters[1].CsharpType);
    }

    [Fact]
    public void ImportsAttrDefParametersFromStringRefParameterClass()
    {
        // StringRefParameter is a well-known AttrOrTypeParameter subclass that maps to C# string.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_NamedAttr : MyP_Attr<\"named\"> {\n" +
            "  let parameters = (ins StringRefParameter<\"the name\">:$value);\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_NamedAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("value", param.Name);
        Assert.Equal("StringRefParameter", param.ConstraintRecordName);
        Assert.Equal("::llvm::StringRef", param.CppType);
        Assert.Equal("std::string", param.CppStorageType);
        Assert.Equal("the name", param.Summary);
        Assert.Equal("string", param.CsharpType);
        // StringRefParameter with no explicit default → no default value.
        Assert.False(param.HasDefaultValue);
    }

    [Fact]
    public void ImportsAttrModelsSeparatelyFromAttrDefModels()
    {
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_FooAttr : MyP_Attr<\"foo\">;\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var prelude = Assert.Single(dialects, static d => d.Name == "prelude");
        var dialect = Assert.Single(dialects, static d => d.Name == "myp");

        var boolAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "BoolAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.IntegerAttr", boolAttr.CsharpStorageType);
        Assert.Equal("bool", boolAttr.CsharpReturnType);
        Assert.Equal("$_self.Value.ToUInt64() != 0", boolAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.Bool($0)", boolAttr.CsharpConstBuilderCall);

        var i32Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "I32Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.IntegerAttr", i32Attr.CsharpStorageType);
        Assert.Equal("uint", i32Attr.CsharpReturnType);
        Assert.Equal("(uint)$_self.Value.ToUInt64()", i32Attr.CsharpConvertFromStorage);

        var si32Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "SI32Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.IntegerAttr", si32Attr.CsharpStorageType);
        Assert.Equal("int", si32Attr.CsharpReturnType);
        Assert.Equal("(int)$_self.Value.ToInt64()", si32Attr.CsharpConvertFromStorage);

        var ui32Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "UI32Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.IntegerAttr", ui32Attr.CsharpStorageType);
        Assert.Equal("uint", ui32Attr.CsharpReturnType);
        Assert.Equal("(uint)$_self.Value.ToUInt64()", ui32Attr.CsharpConvertFromStorage);

        var f16Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "F16Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.FloatAttr", f16Attr.CsharpStorageType);
        Assert.Equal("global::MLIR.Numerics.ApFloat", f16Attr.CsharpReturnType);
        Assert.Equal("$_self.Value", f16Attr.CsharpConvertFromStorage);

        var f32Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "F32Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.FloatAttr", f32Attr.CsharpStorageType);
        Assert.Equal("global::MLIR.Numerics.ApFloat", f32Attr.CsharpReturnType);
        Assert.Equal("$_self.Value", f32Attr.CsharpConvertFromStorage);

        var f64Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "F64Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.FloatAttr", f64Attr.CsharpStorageType);
        Assert.Equal("global::MLIR.Numerics.ApFloat", f64Attr.CsharpReturnType);
        Assert.Equal("$_self.Value", f64Attr.CsharpConvertFromStorage);

        var bf16Attr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "BF16Attr");
        Assert.Equal("global::MLIR.Dialects.Builtin.FloatAttr", bf16Attr.CsharpStorageType);
        Assert.Equal("global::MLIR.Numerics.ApFloat", bf16Attr.CsharpReturnType);
        Assert.Equal("$_self.Value", bf16Attr.CsharpConvertFromStorage);

        var strAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "StrAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.StringAttr", strAttr.CsharpStorageType);
        Assert.Equal("string", strAttr.CsharpReturnType);
        Assert.Equal("$_self.Value", strAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.String($0)", strAttr.CsharpConstBuilderCall);

        var typeAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "TypeAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.TypeAttr", typeAttr.CsharpStorageType);
        Assert.Equal("global::MLIR.Semantics.TypeReference", typeAttr.CsharpReturnType);
        Assert.Equal("$_self.Value", typeAttr.CsharpConvertFromStorage);

        var unitAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "UnitAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.UnitAttr", unitAttr.CsharpStorageType);
        Assert.Equal("bool", unitAttr.CsharpReturnType);
        Assert.True(unitAttr.IsOptional);
        Assert.Equal("false", unitAttr.CsharpDefaultValue);
        Assert.Null(unitAttr.CsharpConstBuilderCall);

        var arrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "ArrayAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.ArrayAttr", arrayAttr.CsharpStorageType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>", arrayAttr.CsharpReturnType);
        Assert.Equal("$_self.Value", arrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.Array($0)", arrayAttr.CsharpConstBuilderCall);

        var i32ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "I32ArrayAttr");
        Assert.Equal(AttributeConstraintKind.TypedArrayAttribute, i32ArrayAttr.Kind);
        Assert.Equal("I32Attr", i32ArrayAttr.ElementConstraintRecordName);
        Assert.Equal("global::MLIR.Dialects.Builtin.ArrayAttr", i32ArrayAttr.CsharpStorageType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<int>", i32ArrayAttr.CsharpReturnType);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => (int)((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())", i32ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.I32(unchecked((uint)element)))", i32ArrayAttr.CsharpConstBuilderCall);

        var denseI32ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseI32ArrayAttr");
        Assert.Equal(AttributeConstraintKind.DenseIntegerArrayAttribute, denseI32ArrayAttr.Kind);
        Assert.Equal("global::MLIR.Dialects.Builtin.DenseArrayAttr", denseI32ArrayAttr.CsharpStorageType);
        Assert.Equal("global::System.ReadOnlySpan<int>", denseI32ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>($_self.RawData.Span)", denseI32ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseI32($0)", denseI32ArrayAttr.CsharpConstBuilderCall);

        var denseBoolArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseBoolArrayAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.DenseArrayAttr", denseBoolArrayAttr.CsharpStorageType);
        Assert.Equal("global::System.ReadOnlySpan<bool>", denseBoolArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>($_self.RawData.Span)", denseBoolArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseBool($0)", denseBoolArrayAttr.CsharpConstBuilderCall);

        var denseI8ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseI8ArrayAttr");
        Assert.Equal("global::System.ReadOnlySpan<sbyte>", denseI8ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>($_self.RawData.Span)", denseI8ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseI8($0)", denseI8ArrayAttr.CsharpConstBuilderCall);

        var denseI16ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseI16ArrayAttr");
        Assert.Equal("global::System.ReadOnlySpan<short>", denseI16ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>($_self.RawData.Span)", denseI16ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseI16($0)", denseI16ArrayAttr.CsharpConstBuilderCall);

        var denseI64ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseI64ArrayAttr");
        Assert.Equal("global::System.ReadOnlySpan<long>", denseI64ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>($_self.RawData.Span)", denseI64ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseI64($0)", denseI64ArrayAttr.CsharpConstBuilderCall);

        var denseF32ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseF32ArrayAttr");
        Assert.Equal("global::System.ReadOnlySpan<float>", denseF32ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>($_self.RawData.Span)", denseF32ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseF32($0)", denseF32ArrayAttr.CsharpConstBuilderCall);

        var denseF64ArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DenseF64ArrayAttr");
        Assert.Equal("global::System.ReadOnlySpan<double>", denseF64ArrayAttr.CsharpReturnType);
        Assert.Equal("global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, double>($_self.RawData.Span)", denseF64ArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.DenseF64($0)", denseF64ArrayAttr.CsharpConstBuilderCall);

        var symbolRefAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "SymbolRefAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.SymbolRefAttr", symbolRefAttr.CsharpStorageType);
        Assert.Equal("global::MLIR.Dialects.Builtin.SymbolRefAttr", symbolRefAttr.CsharpReturnType);
        Assert.Equal("$_self", symbolRefAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.SymbolRef($0)", symbolRefAttr.CsharpConstBuilderCall);

        var flatSymbolRefAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "FlatSymbolRefAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.SymbolRefAttr", flatSymbolRefAttr.CsharpStorageType);
        Assert.Equal("string", flatSymbolRefAttr.CsharpReturnType);
        Assert.Equal("$_self.RootReference", flatSymbolRefAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.ConstantAttributeFactory.FlatSymbolRef($0)", flatSymbolRefAttr.CsharpConstBuilderCall);

        var dictArrayAttr = Assert.Single(prelude.Attrs, static attr => attr.RecordName == "DictArrayAttr");
        Assert.Equal("global::MLIR.Dialects.Builtin.ArrayAttr", dictArrayAttr.CsharpStorageType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection>", dictArrayAttr.CsharpReturnType);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.DictionaryAttr)itemValue).Value)", dictArrayAttr.CsharpConvertFromStorage);
        Assert.Equal("global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.DictionaryAttr(element, null))", dictArrayAttr.CsharpConstBuilderCall);

        Assert.Single(dialect.Attributes, static attr => attr.RecordName == "MyP_FooAttr");
        Assert.DoesNotContain(dialect.Attrs, static attr => attr.RecordName == "MyP_FooAttr");
    }

    [Fact]
    public void ImportsBuiltinAttributeParameterCsharpMetadataFromPreludeOverlay()
    {
        const string source =
            "include \"mlir/IR/BuiltinAttributes.td\"\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var builtin = Assert.Single(dialects, static d => d.Name == "builtin");

        var denseArrayAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_DenseArray");
        Assert.Equal("global::MLIR.Semantics.TypeReference", denseArrayAttr.Parameters[0].CsharpType);
        Assert.Equal("long", denseArrayAttr.Parameters[1].CsharpType);
        Assert.Equal("global::System.ReadOnlyMemory<byte>", denseArrayAttr.Parameters[2].CsharpType);

        var denseTypedElementsAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_DenseTypedElementsAttr");
        Assert.Equal("global::MLIR.Semantics.TypeReference", denseTypedElementsAttr.Parameters[0].CsharpType);
        Assert.Equal("global::MLIR.Semantics.AttributeValue", denseTypedElementsAttr.Parameters[1].CsharpType);
        Assert.Equal("new global::MLIR.Dialects.Attributes.Collections.ElementsAttributeAssemblyFormat()", denseTypedElementsAttr.CsharpAssemblyFormat);

        var denseStringElementsAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_DenseStringElementsAttr");
        Assert.Equal("global::MLIR.Semantics.TypeReference", denseStringElementsAttr.Parameters[0].CsharpType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<string>", denseStringElementsAttr.Parameters[1].CsharpType);

        var denseResourceElementsAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_DenseResourceElementsAttr");
        Assert.Equal("global::MLIR.Semantics.TypeReference", denseResourceElementsAttr.Parameters[0].CsharpType);
        Assert.Equal("string", denseResourceElementsAttr.Parameters[1].CsharpType);

        var sparseElementsAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_SparseElementsAttr");
        Assert.Equal("global::MLIR.Semantics.TypeReference", sparseElementsAttr.Parameters[0].CsharpType);
        Assert.Equal("global::MLIR.Semantics.AttributeValue", sparseElementsAttr.Parameters[1].CsharpType);
        Assert.Equal("global::MLIR.Semantics.AttributeValue", sparseElementsAttr.Parameters[2].CsharpType);

        var symbolRefAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "Builtin_SymbolRefAttr");
        Assert.Equal("string", symbolRefAttr.Parameters[0].CsharpType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<string>", symbolRefAttr.Parameters[1].CsharpType);

        var stridedLayoutAttr = Assert.Single(builtin.Attributes, static attr => attr.RecordName == "StridedLayoutAttr");
        Assert.Equal("long?", stridedLayoutAttr.Parameters[0].CsharpType);
        Assert.Equal("global::System.Collections.Generic.IReadOnlyList<long?>", stridedLayoutAttr.Parameters[1].CsharpType);
    }

    [Fact]
    public void ImportsTypeDefParametersFromMixedParameterClasses()
    {
        // TypeDef with mixed parameter classes: plain string and named class.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Type<string name> : TypeDef<MyP_Dialect, name> {\n" +
            "  let typeName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_VecType : MyP_Type<\"vec\"> {\n" +
            "  let parameters = (ins \"unsigned\":$rank, StringRefParameter<\"element kind\">:$kind);\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var type = Assert.Single(dialect.Types, static t => t.RecordName == "MyP_VecType");

        Assert.Equal(2, type.Parameters.Count);

        var rankParam = type.Parameters[0];
        Assert.Equal("rank", rankParam.Name);
        Assert.Equal("unsigned", rankParam.CppType);
        Assert.Null(rankParam.ConstraintRecordName);
        Assert.Null(rankParam.CsharpType);

        var kindParam = type.Parameters[1];
        Assert.Equal("kind", kindParam.Name);
        Assert.Equal("StringRefParameter", kindParam.ConstraintRecordName);
        Assert.Equal("::llvm::StringRef", kindParam.CppType);
        Assert.Equal("std::string", kindParam.CppStorageType);
        Assert.Equal("string", kindParam.CsharpType);
        Assert.Equal("element kind", kindParam.Summary);
    }

    [Fact]
    public void ImportsTypeDefSummaryDescriptionAndAssemblyFormat()
    {
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Type<string name> : TypeDef<MyP_Dialect, name> {\n" +
            "  let typeName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_OpaqueType : MyP_Type<\"opaque\"> {\n" +
            "  let summary = \"an opaque type\";\n" +
            "  let description = [{A type with a custom printed form.}];\n" +
            "  let parameters = (ins \"unsigned\":$width);\n" +
            "  let assemblyFormat = \"`<` $width `>`\";\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var type = Assert.Single(dialect.Types, static t => t.RecordName == "MyP_OpaqueType");

        Assert.Equal("an opaque type", type.Summary);
        Assert.Contains("custom printed form", type.Description);
        Assert.NotNull(type.AssemblyFormat);
        var elements = type.AssemblyFormat!.Elements;
        Assert.Equal(3, elements.Count);
        Assert.IsType<LiteralChunk>(elements[0]);
        Assert.IsType<VariableChunk>(elements[1]);
        Assert.IsType<LiteralChunk>(elements[2]);
    }

    [Fact]
    public void ImportsAttrDefParameterWithOptionalDefaultValue()
    {
        // StringRefParameter with an explicit default value makes the parameter optional.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_OptAttr : MyP_Attr<\"opt\"> {\n" +
            "  let parameters = (ins StringRefParameter<\"label\", \"default\">:$label);\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_OptAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("label", param.Name);
        Assert.Equal("default", param.DefaultValue);
        Assert.True(param.HasDefaultValue);
        Assert.Equal("string", param.CsharpType);
    }

    [Fact]
    public void ImportsAttrOrTypeParameterExtensionCsharpTypeFromUserDefinedClass()
    {
        // User-defined parameter classes that inherit MLIRNet_AttrOrTypeParameterExtension
        // allow callers to declare their own C# type mapping.
        const string source =
            "include \"mlir/Extensions/IR/MLIRNetExtensions.td\"\n" +
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyCustomIntParam<string desc> :\n" +
            "    AttrOrTypeParameter<\"MyIntType\", desc>,\n" +
            "    MLIRNet_AttrOrTypeParameterExtension {\n" +
            "  let csharpType = \"global::MyNamespace.MyInt\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_CustomAttr : MyP_Attr<\"custom\"> {\n" +
            "  let parameters = (ins MyCustomIntParam<\"width\">:$width);\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_CustomAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("width", param.Name);
        Assert.Equal("MyCustomIntParam", param.ConstraintRecordName);
        Assert.Equal("MyIntType", param.CppType);
        Assert.Equal("global::MyNamespace.MyInt", param.CsharpType);
        Assert.Equal("width", param.Summary);
    }

    [Fact]
    public void ImportsAttrDefWithNoParametersAsEmptyParameterList()
    {
        // An AttrDef with no parameters field (or empty parameters) should have an empty list.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_UnitAttr : MyP_Attr<\"unit\">;\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_UnitAttr");

        Assert.Empty(attr.Parameters);
    }

    [Fact]
    public void ImportsAttrDefAssemblyFormatString()
    {
        // An AttrDef with an assemblyFormat string should expose it in the model.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_OpaqueAttr : MyP_Attr<\"opaque\"> {\n" +
            "  let parameters = (ins StringRefParameter<\"the value\">:$value);\n" +
            "  let assemblyFormat = \"`<` $value `>`\";\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_OpaqueAttr");

        // Parameters should be imported.
        var param = Assert.Single(attr.Parameters);
        Assert.Equal("value", param.Name);
        Assert.Equal("string", param.CsharpType);

        // Assembly format should be imported and parsed.
        Assert.NotNull(attr.AssemblyFormat);
        var elements = attr.AssemblyFormat!.Elements;
        Assert.Equal(3, elements.Count);
        Assert.IsType<LiteralChunk>(elements[0]);
        Assert.IsType<VariableChunk>(elements[1]);
        Assert.IsType<LiteralChunk>(elements[2]);
        var variable = (VariableChunk)elements[1];
        Assert.Equal("value", variable.Name);
    }

    [Fact]
    public void ImportsAttrOrTypeParameterExtensionCsharpParserAndPrinter()
    {
        // User-defined parameter classes can specify csharpParser and csharpPrinter.
        const string source =
            "class MyIdParam<string desc> : AttrOrTypeParameter<\"std::string\", desc>;\n" +
            "extends MyIdParam : MLIRNet_AttrOrTypeParameterExtension {\n" +
            "  let csharpType = \"string\";\n" +
            "  let csharpParser = \"$_parser.ParseId()\";\n" +
            "  let csharpPrinter = \"Fmt($_self)\";\n" +
            "}\n" +
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_IdAttr : MyP_Attr<\"id\"> {\n" +
            "  let parameters = (ins MyIdParam<\"the id\">:$name);\n" +
            "  let assemblyFormat = \"`<` $name `>`\";\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_IdAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("name", param.Name);
        Assert.Equal("string", param.CsharpType);
        Assert.Equal("$_parser.ParseId()", param.CsharpParser);
        Assert.Equal("Fmt($_self)", param.CsharpPrinter);
    }

    [Fact]
    public void ImportsAttrOrTypeParameterExtensionCsharpExtractorAndDefault()
    {
        // User-defined parameter classes can specify csharpExtractor and csharpDefault.
        const string source =
            "class MyIdParam<string desc> : AttrOrTypeParameter<\"std::string\", desc>;\n" +
            "extends MyIdParam : MLIRNet_AttrOrTypeParameterExtension {\n" +
            "  let csharpType = \"string\";\n" +
            "  let csharpExtractor = \"Extract($_syntax)\";\n" +
            "  let csharpDefault = \"string.Empty\";\n" +
            "}\n" +
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_IdAttr : MyP_Attr<\"id\"> {\n" +
            "  let parameters = (ins MyIdParam<\"the id\">:$name);\n" +
            "  let assemblyFormat = \"`<` $name `>`\";\n" +
            "};\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_IdAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("name", param.Name);
        Assert.Equal("string", param.CsharpType);
        Assert.Equal("Extract($_syntax)", param.CsharpExtractor);
        Assert.Equal("string.Empty", param.CsharpDefault);
    }

    [Fact]
    public void CsharpParametersStringLiteralOverridesCsharpTypeFromParametersDag()
    {
        // When csharpParameters is present and an entry is a string literal,
        // that string is used directly as the C# type for the matching parameter.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_ValueAttr : MyP_Attr<\"value\"> {\n" +
            "  let parameters = (ins \"uint64_t\":$value);\n" +
            "};\n" +
            "extends MyP_ValueAttr : MLIRNet_AttrOrTypeDefExtension {\n" +
            "  let csharpParameters = (ins \"System.Numerics.BigInteger\":$value);\n" +
            "}\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_ValueAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("value", param.Name);
        // C++ type is unchanged — parameters is still the source of truth for C++ semantics.
        Assert.Equal("uint64_t", param.CppType);
        // The csharpParameters string literal becomes the C# type.
        Assert.Equal("System.Numerics.BigInteger", param.CsharpType);
        // No parser/extractor/printer are inferred from a plain string literal.
        Assert.Null(param.CsharpParser);
        Assert.Null(param.CsharpExtractor);
        Assert.Null(param.CsharpPrinter);
    }

    [Fact]
    public void CsharpParametersRecordEntryResolvesCsharpExtensionFields()
    {
        // When csharpParameters contains a parameter class instance,
        // the C# extension fields are resolved from that class.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_LabelAttr : MyP_Attr<\"label\"> {\n" +
            "  let parameters = (ins \"std::string\":$label);\n" +
            "};\n" +
            "extends MyP_LabelAttr : MLIRNet_AttrOrTypeDefExtension {\n" +
            "  let csharpParameters = (ins StringRefParameter<\"the label\">:$label);\n" +
            "}\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_LabelAttr");

        var param = Assert.Single(attr.Parameters);
        Assert.Equal("label", param.Name);
        // C++ type is preserved from the parameters dag.
        Assert.Equal("std::string", param.CppType);
        // C# type and extension fields come from StringRefParameter via csharpParameters.
        Assert.Equal("string", param.CsharpType);
        Assert.Equal("StringAttributeValueSyntax", param.CsharpSyntaxType);
        Assert.NotNull(param.CsharpParser);
        Assert.NotNull(param.CsharpExtractor);
        Assert.NotNull(param.CsharpPrinter);
    }

    [Fact]
    public void CsharpParametersMixedEntriesOverrideSomeParameters()
    {
        // csharpParameters can contain a mix of string and record entries,
        // each overriding only the named parameter.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_PairAttr : MyP_Attr<\"pair\"> {\n" +
            "  let parameters = (ins StringRefParameter<\"the name\">:$name, \"uint64_t\":$value);\n" +
            "};\n" +
            "extends MyP_PairAttr : MLIRNet_AttrOrTypeDefExtension {\n" +
            "  let csharpParameters = (ins\n" +
            "    StringRefParameter<\"the name\">:$name,\n" +
            "    \"System.Numerics.BigInteger\":$value\n" +
            "  );\n" +
            "}\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_PairAttr");

        Assert.Equal(2, attr.Parameters.Count);

        var nameParam = attr.Parameters[0];
        Assert.Equal("name", nameParam.Name);
        Assert.Equal("string", nameParam.CsharpType);

        var valueParam = attr.Parameters[1];
        Assert.Equal("value", valueParam.Name);
        Assert.Equal("uint64_t", valueParam.CppType);
        Assert.Equal("System.Numerics.BigInteger", valueParam.CsharpType);
        // A plain string entry carries no parser/extractor/printer.
        Assert.Null(valueParam.CsharpParser);
    }

    [Fact]
    public void CsharpParametersDoesNotAffectParametersWithoutMatchingEntry()
    {
        // Parameters not mentioned in csharpParameters retain their original C# metadata.
        const string source =
            "def MyP_Dialect : Dialect {\n" +
            "  let name = \"myp\";\n" +
            "  let cppNamespace = \"::mlir::myp\";\n" +
            "};\n" +
            "class MyP_Attr<string name> : AttrDef<MyP_Dialect, name> {\n" +
            "  let attrName = \"myp.\" # name;\n" +
            "};\n" +
            "def MyP_TripleAttr : MyP_Attr<\"triple\"> {\n" +
            "  let parameters = (ins\n" +
            "    StringRefParameter<\"first\">:$first,\n" +
            "    \"uint64_t\":$second,\n" +
            "    APIntParameter<\"third\">:$third\n" +
            "  );\n" +
            "};\n" +
            "extends MyP_TripleAttr : MLIRNet_AttrOrTypeDefExtension {\n" +
            "  let csharpParameters = (ins \"int\":$second);\n" +
            "}\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var dialect = Assert.Single(dialects, static d => d.Name == "myp");
        var attr = Assert.Single(dialect.Attributes, static a => a.RecordName == "MyP_TripleAttr");

        Assert.Equal(3, attr.Parameters.Count);

        // first: not in csharpParameters — retains C# metadata from StringRefParameter extension.
        var first = attr.Parameters[0];
        Assert.Equal("first", first.Name);
        Assert.Equal("string", first.CsharpType);

        // second: overridden by csharpParameters string literal.
        var second = attr.Parameters[1];
        Assert.Equal("second", second.Name);
        Assert.Equal("int", second.CsharpType);

        // third: not in csharpParameters — retains C# metadata from APIntParameter extension.
        var third = attr.Parameters[2];
        Assert.Equal("third", third.Name);
        Assert.Equal("global::MLIR.Numerics.ApInt", third.CsharpType);
    }

    [Fact]
    public void AttrModelLegacyPlaceholdersNormalizeToCanonicalTemplates()
    {
        // Verify that AttrModel fields using legacy placeholder spellings ($_self, $0)
        // are exposed as CodeTemplate objects with canonical ${self} and ${value} spellings.
        // The raw string properties retain their original spellings for round-trip fidelity;
        // the *Template properties perform the normalization.
        const string source =
            "include \"mlir/IR/BuiltinAttributes.td\"\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var prelude = dialects[0];

        // DenseI32ArrayAttr uses $0 in csharpConstBuilderCall and $_self in csharpConvertFromStorage.
        var denseI32 = Assert.Single(prelude.Attrs, static a => a.RecordName == "DenseI32ArrayAttr");

        // Raw string retains legacy spelling.
        Assert.Contains("$_self", denseI32.CsharpConvertFromStorage);
        Assert.Contains("$0", denseI32.CsharpConstBuilderCall);

        // CodeTemplate properties normalize to canonical spelling.
        var convertTemplate = denseI32.CsharpConvertFromStorageTemplate;
        Assert.NotNull(convertTemplate);
        Assert.DoesNotContain("$_self", convertTemplate!.Text);
        Assert.Contains("${self}", convertTemplate.Text);
        Assert.Equal(["self"], convertTemplate.PlaceholderNames);

        var constBuilderTemplate = denseI32.CsharpConstBuilderCallTemplate;
        Assert.NotNull(constBuilderTemplate);
        Assert.DoesNotContain("$0", constBuilderTemplate!.Text);
        Assert.Contains("${value}", constBuilderTemplate.Text);
        Assert.Equal(["value"], constBuilderTemplate.PlaceholderNames);
    }

    [Fact]
    public void AttrOrTypeParameterModelLegacyPlaceholdersNormalizeToCanonicalTemplates()
    {
        // Verify that AttrOrTypeParameterModel fields using legacy placeholder spellings
        // ($_parser, $_syntax, $_self) are exposed as CodeTemplate objects with canonical
        // ${parser}, ${syntax}, ${self} spellings via the *Template properties.
        const string source =
            "include \"mlir/IR/BuiltinAttributes.td\"\n";

        var dialects = DialectImporter.Import(GeneratorTestHelpers.LoadTableGenWithUpstreamPrelude(source).Evaluate());

        var builtin = Assert.Single(dialects, static d => d.Name == "builtin");

        // StringRefParameter uses $_parser, $_syntax, $_self in the prelude extension overlay.
        // Find an attribute that uses StringRefParameter to verify normalization.
        var stridedLayoutAttr = Assert.Single(builtin.Attributes, static a => a.RecordName == "StridedLayoutAttr");

        // The first parameter (offset) uses APIntParameter which has csharpParser/$_parser.
        // The parameters are imported with legacy spellings in the raw string fields.
        // The Template properties must expose canonical spellings.
        foreach (var param in stridedLayoutAttr.Parameters)
        {
            if (!string.IsNullOrEmpty(param.CsharpParser))
            {
                // Raw string retains legacy spelling OR already canonical — either way,
                // the template must use canonical ${parser}.
                var parserTemplate = param.CsharpParserTemplate;
                Assert.NotNull(parserTemplate);
                Assert.DoesNotContain("$_parser", parserTemplate!.Text);
                if (parserTemplate.PlaceholderNames.Count > 0)
                {
                    Assert.Contains("parser", parserTemplate.PlaceholderNames);
                }
            }

            if (!string.IsNullOrEmpty(param.CsharpExtractor))
            {
                var extractorTemplate = param.CsharpExtractorTemplate;
                Assert.NotNull(extractorTemplate);
                Assert.DoesNotContain("$_syntax", extractorTemplate!.Text);
            }

            if (!string.IsNullOrEmpty(param.CsharpPrinter))
            {
                var printerTemplate = param.CsharpPrinterTemplate;
                Assert.NotNull(printerTemplate);
                Assert.DoesNotContain("$_self", printerTemplate!.Text);
            }
        }
    }
}
