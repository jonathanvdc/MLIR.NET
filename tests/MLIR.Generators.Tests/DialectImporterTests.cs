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

        var dialect = Assert.Single(dialects);
        var constantOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.constant");
        var addiOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.addi");
        var attribute = Assert.Single(dialect.Attributes, static attr => attr.Name == "miniarith.i32");
        var type = Assert.Single(dialect.Types, static typeModel => typeModel.Name == "miniarith.i32");
        var constraint = Assert.Single(dialect.AttributeConstraints, static attrConstraint => attrConstraint.Name == "MiniArith_I32ConstraintAttr");

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
        Assert.Equal(["Pure"], constantOp.Traits);
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
        Assert.Equal(["Pure", "Commutative"], addiOp.Traits);
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

        var dialect = Assert.Single(dialects);
        var op = Assert.Single(dialect.Operations);

        // cppClassName="" must fall back to the record name, not produce an empty class name.
        Assert.Equal("MyDialect_FooOp", op.ClassName);
        // summary="" must be treated as absent (null).
        Assert.Null(op.Summary);
        // assemblyFormat="" must be treated as absent (no custom assembly format).
        Assert.Null(op.AssemblyFormat);
    }
}
