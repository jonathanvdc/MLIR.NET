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
            "namespace MLIR.Dialects.Miniarith;",
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

        Assert.Contains(result.GeneratedSources, static source => source.HintName == "PreludeDialectRegistration.g.cs");
        Assert.DoesNotContain(result.GeneratedSources, static source => source.HintName == "MiniarithDialectRegistration.g.cs");
        Assert.Contains("MiniArith_BrokenOp", diagnostic.GetMessage());
        Assert.Contains("No body field was generated for operand 'lhs'", diagnostic.GetMessage());
    }

    [Fact]
    public void GeneratesBuiltinTypeConstraintWrappersAndRegistersSelfIdentifyingOnes()
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            (
                "miniarith.td",
                ComposeMiniArithSource(
                    [
                        "def MiniArith_IdOp : MiniArith_Op<\"id\", [Pure]> {",
                        "  let arguments = (ins AnyTensor:$input, AnyVectorOfAnyRank:$vector, AnyMemRef:$memory, FunctionType:$callee);",
                        "  let results = (outs AnyTuple:$result);",
                        "  let assemblyFormat = \"$input `,` $vector `,` $memory `,` $callee attr-dict `:` type($result)\";",
                        "};",
                    ])));

        var preludeSource = Assert.Single(
            generatedSources.Where(static r => r.HintName == "PreludeDialectRegistration.g.cs")).SourceText.ToString();
        var registrationSource = Assert.Single(
            generatedSources.Where(static r => r.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        AssertContainsAll(
            preludeSource,
            "namespace MLIR.Dialects.Prelude;",
            "public static class PreludeDialectRegistration",
            "public static partial class I32ConstraintTypeReference",
            "public static partial class F32ConstraintTypeReference",
            "public static partial class IndexConstraintTypeReference",
            "public static partial class NoneTypeConstraintTypeReference",
            "public static partial class AnyTupleConstraintTypeReference",
            "public static partial class FunctionTypeConstraintTypeReference",
            "public static partial class AnyTensorConstraintTypeReference",
            "public static partial class AnyVectorOfAnyRankConstraintTypeReference",
            "public static partial class AnyMemRefConstraintTypeReference",
            "dialect.AddTypeConstraint(I32ConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(F32ConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(IndexConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(NoneTypeConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(AnyTupleConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(FunctionTypeConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(AnyTensorConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(AnyVectorOfAnyRankConstraintTypeReference.TypeConstraintDefinition);",
            "dialect.AddTypeConstraint(AnyMemRefConstraintTypeReference.TypeConstraintDefinition);");
        Assert.DoesNotContain("global::MLIR.Dialects.Prelude.PreludeDialectRegistration.Create", preludeSource);
        // Type constraints must not expose TypeDefinition and must not register as dialect types.
        Assert.DoesNotContain("I32ConstraintTypeReference.TypeDefinition", preludeSource);
        Assert.DoesNotContain("dialect.AddType(I32ConstraintTypeReference", preludeSource);

        AssertContainsAll(
            registrationSource,
            "public static class MiniarithDialectRegistration",
            "Dialect.Create(\"miniarith\", dialect =>",
            "global::MLIR.Dialects.Prelude.PreludeDialectRegistration.Create");
        Assert.DoesNotContain("I32ConstraintTypeReference", registrationSource);
    }

    [Fact]
    public void GeneratesListPropertyForVariadicResult()
    {
        // Use a non-shadowing name so the variadic list property is always emitted.
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_CallOp : MiniArith_Op<\"call\", []> {",
                "  let results = (outs Variadic<AnyType>:$myResults);",
                "};",
            ]);

        // A variadic result should be exposed as a read-only list, not as a single OperationResult.
        AssertContainsAll(
            registrationSource,
            "global::System.Collections.Generic.IReadOnlyList<OperationResult>",
            "base.Results.Skip(");

        // A variadic result must not produce a single-value ResultValue property.
        Assert.DoesNotContain("public OperationResult ResultValue", registrationSource);
    }

    [Fact]
    public void DoesNotGenerateConvenienceAliasForVariadicResult()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_CallOp : MiniArith_Op<\"call\", []> {",
                "  let results = (outs Variadic<AnyType>:$myResults);",
                "};",
            ]);

        // No single-value alias should be emitted for a variadic result.
        Assert.DoesNotContain("public OperationResult ResultValue", registrationSource);
        Assert.DoesNotContain("=> ResultValue;", registrationSource);
    }

    [Fact]
    public void DoesNotGenerateRedundantShadowingResultsProperty()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_CallOp : MiniArith_Op<\"call\", []> {",
                "  let results = (outs Variadic<AnyType>:$results);",
                "};",
            ]);

        // A variadic result named "results" whose property name would shadow the inherited base.Results
        // must not be emitted—it produces the same collection as the base property and adds no value.
        Assert.DoesNotContain("public new", registrationSource);
        Assert.DoesNotContain("base.Results.Skip(", registrationSource);
        Assert.DoesNotContain("public OperationResult ResultValue", registrationSource);
    }

    [Fact]
    public void KeepsShadowingOperandsPropertyBecauseItChangesType()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_CallOp : MiniArith_Op<\"call\", []> {",
                "  let arguments = (ins Variadic<AnyType>:$operands);",
                "};",
            ]);

        // A variadic operand named "operands" changes the return type from IReadOnlyList<OpOperand>
        // (base) to IReadOnlyList<Value> (generated), which is a meaningful type transformation that
        // downstream assembly-format code may depend on. The shadowing property is retained in this case.
        Assert.Contains("public new", registrationSource);
        Assert.Contains("base.Operands.Skip(", registrationSource);
    }

    [Fact]
    public void GeneratesResultValuePropertyForSingleNonVariadicResult()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        // A single non-variadic result should still produce a ResultValue convenience property.
        Assert.Contains("public OperationResult ResultValue", registrationSource);
    }
}
