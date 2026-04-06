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
        Assert.Equal("tuple", tuple.CanonicalTypeName);
        Assert.Equal(TypeConstraintKind.FunctionType, function.Kind);
        Assert.Equal("function", function.CanonicalTypeName);
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
}
