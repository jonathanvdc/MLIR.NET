namespace MLIR.Tests;

using System.Collections.Generic;
using System.Linq;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;

/// <summary>
/// Regression tests verifying that builtin collection types and primitive types can be constructed
/// programmatically without attached syntax and printed correctly by <see cref="ConcreteSyntaxBuilder"/>.
/// </summary>
public sealed class SyntaxlessTypeTests
{
    private static ConcreteSyntaxBuilder.ConcreteSyntaxBuilderOptions ReplaceOptions =>
        new(existingSyntaxHandling: ConcreteSyntaxBuilder.ExistingSyntaxHandling.ReplaceExistingSyntax);

    // --- Generated FunctionType ---

    [Fact]
    public void FunctionType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Function([TypeFactory.I32], [TypeFactory.I64]);
        Assert.Null(type.Syntax);
    }

    [Theory]
    [InlineData("(i32) -> i64")]
    public void FunctionType_PrintsCorrectlyWithoutSyntax(string expected)
    {
        var type = TypeFactory.Function([TypeFactory.I32], [TypeFactory.I64]);
        Assert.Equal(expected, Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void FunctionType_WithMultipleInputsAndResults_PrintsCorrectly()
    {
        var type = TypeFactory.Function(
            [TypeFactory.I32, TypeFactory.I64],
            [TypeFactory.I32, TypeFactory.I64]);
        Assert.Equal("(i32, i64) -> (i32, i64)", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void FunctionType_ZeroResults_PrintsWithEmptyResultList()
    {
        var type = TypeFactory.Function([TypeFactory.I32], []);
        Assert.Equal("(i32) -> ()", Printer.PrintType(type, ReplaceOptions));
    }

    // --- Generated TupleType ---

    [Fact]
    public void TupleType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Tuple(TypeFactory.I32, TypeFactory.I64);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void TupleType_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.Tuple(TypeFactory.I32, TypeFactory.I64);
        Assert.Equal("tuple<i32, i64>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void TupleType_EmptyTuple_PrintsCorrectly()
    {
        var type = TypeFactory.Tuple();
        Assert.Equal("tuple<>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- RankedTensorType ---

    [Fact]
    public void RankedTensorType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Tensor([2L, 3L], TypeFactory.I32);
        Assert.Null(type.Syntax);
    }

    [Theory]
    [InlineData("tensor<2x3xi32>")]
    public void RankedTensorType_PrintsCorrectlyWithoutSyntax(string expected)
    {
        var type = TypeFactory.Tensor([2L, 3L], TypeFactory.I32);
        Assert.Equal(expected, Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void RankedTensorType_DynamicDimension_PrintsCorrectly()
    {
        var type = TypeFactory.Tensor([null, 3L], TypeFactory.I32);
        Assert.Equal("tensor<?x3xi32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void UnrankedTensorType_PrintsCorrectly()
    {
        var type = TypeFactory.UnrankedTensor(TypeFactory.I32);
        Assert.Equal("tensor<*xi32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void RankedTensorType_WithEncoding_PrintsCorrectly()
    {
        var type = TypeFactory.Tensor([2L, 3L], TypeFactory.I32, "#encoding");
        Assert.Equal("tensor<2x3xi32, #encoding>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- VectorType ---

    [Fact]
    public void VectorType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Vector([4L], TypeFactory.F32);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void VectorType_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.Vector([4L], TypeFactory.F32);
        Assert.Equal("vector<4xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void VectorType_MultidimensionalVector_PrintsCorrectly()
    {
        var type = TypeFactory.Vector([2L, 8L], TypeFactory.F32);
        Assert.Equal("vector<2x8xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- MemRefType ---

    [Fact]
    public void MemRefType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.MemRef([10L], TypeFactory.F32);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void MemRefType_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.MemRef([10L], TypeFactory.F32);
        Assert.Equal("memref<10xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void UnrankedMemRefType_PrintsCorrectly()
    {
        var type = TypeFactory.UnrankedMemRef(TypeFactory.I32);
        Assert.Equal("memref<*xi32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void UnrankedMemRefType_WithMemorySpace_PrintsCorrectly()
    {
        var type = TypeFactory.UnrankedMemRef(TypeFactory.I32, "#space");
        Assert.Equal("memref<*xi32, #space>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- IndexType ---

    [Fact]
    public void IndexType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = new IndexType();
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void IndexType_PrintsCorrectlyWithoutSyntax()
    {
        var type = new IndexType();
        Assert.Equal("index", Printer.PrintType(type, ReplaceOptions));
    }

    // --- Nested syntaxless types ---

    [Fact]
    public void FunctionType_WithSyntaxlessTensorElementTypes_PrintsCorrectly()
    {
        // Neither the outer function type nor the inner tensor types carry syntax.
        var tensorType = TypeFactory.Tensor([2L, 3L], TypeFactory.I32);
        var funcType = TypeFactory.Function([tensorType], [tensorType]);

        Assert.Null(tensorType.Syntax);
        Assert.Null(funcType.Syntax);

        Assert.Equal("(tensor<2x3xi32>) -> tensor<2x3xi32>", Printer.PrintType(funcType, ReplaceOptions));
    }

    [Fact]
    public void TupleType_WithSyntaxlessNestedTypes_PrintsCorrectly()
    {
        var innerFunc = TypeFactory.Function([TypeFactory.I32], [TypeFactory.I64]);
        var tuple = TypeFactory.Tuple(innerFunc, TypeFactory.Index);

        Assert.Null(innerFunc.Syntax);
        Assert.Null(tuple.Syntax);

        Assert.Equal("tuple<(i32) -> i64, index>", Printer.PrintType(tuple, ReplaceOptions));
    }

    [Fact]
    public void TensorType_WithSyntaxlessVectorElementType_PrintsCorrectly()
    {
        // Vector element inside tensor, both syntaxless.
        var vectorType = TypeFactory.Vector([4L], TypeFactory.F32);
        var tensorType = TypeFactory.Tensor([2L], vectorType);

        Assert.Null(vectorType.Syntax);
        Assert.Null(tensorType.Syntax);

        Assert.Equal("tensor<2xvector<4xf32>>", Printer.PrintType(tensorType, ReplaceOptions));
    }

    // --- NoneType ---

    [Fact]
    public void NoneType_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = new NoneType();
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void NoneType_PrintsCorrectlyWithoutSyntax()
    {
        var type = new NoneType();
        Assert.Equal("none", Printer.PrintType(type, ReplaceOptions));
    }

    // --- Float types (syntaxless) ---

    [Fact]
    public void Float32Type_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.F32;
        Assert.Null(type.Syntax);
    }

    [Theory]
    [InlineData("f16")]
    [InlineData("f32")]
    [InlineData("f64")]
    [InlineData("bf16")]
    [InlineData("tf32")]
    public void ScalarFloatTypeFactory_PrintsCorrectMnemonicWithoutSyntax(string mnemonic)
    {
        var type = mnemonic switch
        {
            "f16" => (TypeReference)TypeFactory.F16,
            "f32" => TypeFactory.F32,
            "f64" => TypeFactory.F64,
            "bf16" => TypeFactory.BF16,
            "tf32" => TypeFactory.TF32,
            _ => throw new System.Exception("unexpected mnemonic"),
        };
        Assert.Equal(mnemonic, Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void TensorType_WithSyntaxlessFloat32ElementType_PrintsCorrectly()
    {
        var tensorType = TypeFactory.Tensor([4L, 8L], TypeFactory.F32);

        Assert.Null(tensorType.Syntax);
        Assert.Equal("tensor<4x8xf32>", Printer.PrintType(tensorType, ReplaceOptions));
    }

    [Fact]
    public void FunctionType_WithSyntaxlessFloatAndIndexTypes_PrintsCorrectly()
    {
        var funcType = TypeFactory.Function(
            [TypeFactory.F32, TypeFactory.Index],
            [TypeFactory.F64]);

        Assert.Null(funcType.Syntax);
        Assert.Equal("(f32, index) -> f64", Printer.PrintType(funcType, ReplaceOptions));
    }

    [Fact]
    public void TupleType_WithSyntaxlessBFloat16AndNone_PrintsCorrectly()
    {
        var tuple = TypeFactory.Tuple(TypeFactory.BF16, TypeFactory.None);

        Assert.Null(tuple.Syntax);
        Assert.Equal("tuple<bf16, none>", Printer.PrintType(tuple, ReplaceOptions));
    }

    // --- TypeDefinition assembly formats are wired up ---

    [Fact]
    public void AllCollectionTypeDefinitionsHaveAssemblyFormats()
    {
        Assert.NotNull(FunctionType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(TupleType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(RankedTensorType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(UnrankedTensorType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(VectorType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(MemRefType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(UnrankedMemRefType.TypeDefinition.AssemblyFormat);
    }

    [Fact]
    public void AllScalarPrimitiveTypeDefinitionsHaveAssemblyFormats()
    {
        // Generated builtin scalar types must carry an assembly format so ConcreteSyntaxBuilder
        // can synthesize CST for syntaxless instances.
        Assert.NotNull(Float16Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float32Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float64Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(BFloat16Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(FloatTF32Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(IndexType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(NoneType.TypeDefinition.AssemblyFormat);
    }
}
