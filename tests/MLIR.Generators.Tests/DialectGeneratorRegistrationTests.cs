namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.Generators;
using Xunit;

public sealed class DialectGeneratorRegistrationTests : DialectGeneratorTestBase
{
    [Fact]
    public void GeneratesDialectRegistrationTypedNodesAndCustomAssemblyStubs()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def Builtin_Dialect : Dialect {",
                "  let name = \"builtin\";",
                "  let cppNamespace = \"::mlir::builtin\";",
                "};",
                string.Empty,
                "class Builtin_Attr<string name> : AttrDef<Builtin_Dialect, name> { let mnemonic = name; };",
                string.Empty,
                "def BuiltinI32Attr : Builtin_Attr<\"i32\">;",
                string.Empty,
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$value attr-dict\";",
                "};",
                string.Empty,
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "namespace MLIR.Miniarith;",
            "public static class MiniarithDialectRegistration",
            "public sealed class MiniArith_ConstantOp : Operation",
            "public sealed class MiniArith_AddIOp : Operation",
            "public static OperationDefinition OperationDefinition { get; } = CreateOperationDefinition();",
            "public sealed class MiniArith_ConstantOpAssemblyFormat : IOperationAssemblyFormat",
            "public sealed class MiniArith_AddIOpAssemblyFormat : IOperationAssemblyFormat",
            "dialect.AddOperation(MiniArith_ConstantOp.OperationDefinition);",
            "dialect.AddOperation(MiniArith_AddIOp.OperationDefinition);",
            ".WithFactory(static context => new MiniArith_AddIOp(context))",
            ".WithAssemblyFormat(new MiniArith_AddIOpAssemblyFormat())",
            "public MiniArith_ConstantOp(OperationConstructionContext context)",
            "public MiniArith_AddIOp(OperationConstructionContext context)");
        Assert.DoesNotContain("RewriteChildren", registrationSource);
    }

    [Fact]
    public void GeneratesXmlDocCommentsFromSummaryAndDescription()
    {
        var registrationSource = GenerateRegistrationSource(
            "miniarith.td",
            "MiniarithDialectRegistration.g.cs",
            ComposeSource(
                [
                    "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :",
                    "    Op<MiniArith_Dialect, mnemonic, traits>;",
                    string.Empty,
                    "def MiniArith_Dialect : Dialect {",
                    "  let name = \"miniarith\";",
                    "  let cppNamespace = \"::mlir::miniarith\";",
                    "  let summary = \"Mini arithmetic dialect\";",
                    "  let description = [{A dialect for basic integer arithmetic operations.}];",
                    "};",
                    string.Empty,
                    "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                    "  let summary = \"integer constant\";",
                    "  let description = [{Produces a constant integer value.}];",
                    "  let arguments = (ins I32Attr:$value);",
                    "  let results = (outs I32:$result);",
                    "};",
                ]));

        AssertContainsAll(
            registrationSource,
            "/// <summary>integer constant</summary>",
            "/// <remarks>",
            "/// Produces a constant integer value.",
            "/// <summary>Mini arithmetic dialect</summary>",
            "/// A dialect for basic integer arithmetic operations.");
    }

    [Fact]
    public void DoesNotGenerateDocCommentsWhenSummaryAndDescriptionAreAbsent()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        Assert.DoesNotContain("/// <summary>", registrationSource);
        Assert.DoesNotContain("/// <remarks>", registrationSource);
    }

    [Fact]
    public void ReportsDiagnosticWhenEmissionFails()
    {
        var runResult = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            true,
            (
                "miniarith.td",
                ComposeMiniArithSource(
                    [
                        "def MiniArith_BrokenOp : MiniArith_Op<\"broken\", [Pure]> {",
                        "  let arguments = (ins I32:$lhs, I32:$rhs);",
                        "  let results = (outs I32:$result);",
                        "  let assemblyFormat = \"attr-dict\";",
                        "};",
                    ])));

        var result = Assert.Single(runResult.Results);
        var diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "MLIRGEN002"));

        Assert.Empty(result.GeneratedSources);
        Assert.Contains("MiniArith_BrokenOp", diagnostic.GetMessage());
        Assert.Contains("No body field was generated for operand 'lhs'", diagnostic.GetMessage());
    }

    [Fact]
    public void GeneratesBuiltinTypeConstraintWrappersAndRegistersSelfIdentifyingOnes()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_IdOp : MiniArith_Op<\"id\", [Pure]> {",
                "  let arguments = (ins AnyTensor:$input, AnyVectorOfAnyRank:$vector, AnyMemRef:$memory, FunctionType:$callee);",
                "  let results = (outs AnyTuple:$result);",
                "  let assemblyFormat = \"$input `,` $vector `,` $memory `,` $callee attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public sealed class I32ConstraintTypeReference : IntegerTypeReference",
            "public sealed class F32ConstraintTypeReference : FloatTypeReference",
            "public sealed class IndexConstraintTypeReference : IndexTypeReference",
            "public sealed class NoneTypeConstraintTypeReference : NoneTypeReference",
            "public sealed class AnyTupleConstraintTypeReference : TupleTypeReference",
            "public sealed class FunctionTypeConstraintTypeReference : FunctionTypeReference",
            "public sealed class AnyTensorConstraintTypeReference : TensorTypeReference",
            "public sealed class AnyVectorOfAnyRankConstraintTypeReference : VectorTypeReference",
            "public sealed class AnyMemRefConstraintTypeReference : MemRefTypeReference",
            "dialect.AddType(I32ConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(F32ConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(IndexConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(NoneTypeConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(AnyTupleConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(FunctionTypeConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(AnyTensorConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(AnyVectorOfAnyRankConstraintTypeReference.TypeDefinition);",
            "dialect.AddType(AnyMemRefConstraintTypeReference.TypeDefinition);");
    }
}
