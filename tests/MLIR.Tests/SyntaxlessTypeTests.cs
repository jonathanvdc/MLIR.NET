namespace MLIR.Tests;

using System.Collections.Generic;
using System.Linq;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Semantics.Types.Collections;
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

    // --- FunctionTypeReference ---

    [Fact]
    public void FunctionTypeReference_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Function([TypeFactory.I32], [TypeFactory.I64]);
        Assert.Null(type.Syntax);
    }

    [Theory]
    [InlineData("(i32) -> i64")]
    public void FunctionTypeReference_PrintsCorrectlyWithoutSyntax(string expected)
    {
        var type = TypeFactory.Function([TypeFactory.I32], [TypeFactory.I64]);
        Assert.Equal(expected, Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void FunctionTypeReference_WithMultipleInputsAndResults_PrintsCorrectly()
    {
        var type = TypeFactory.Function(
            [TypeFactory.I32, TypeFactory.I64],
            [TypeFactory.I32, TypeFactory.I64]);
        Assert.Equal("(i32, i64) -> (i32, i64)", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void FunctionTypeReference_ZeroResults_PrintsWithEmptyResultList()
    {
        var type = TypeFactory.Function([TypeFactory.I32], []);
        Assert.Equal("(i32) -> ()", Printer.PrintType(type, ReplaceOptions));
    }

    // --- TupleTypeReference ---

    [Fact]
    public void TupleTypeReference_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Tuple(TypeFactory.I32, TypeFactory.I64);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void TupleTypeReference_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.Tuple(TypeFactory.I32, TypeFactory.I64);
        Assert.Equal("tuple<i32, i64>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void TupleTypeReference_EmptyTuple_PrintsCorrectly()
    {
        var type = TypeFactory.Tuple();
        Assert.Equal("tuple<>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- TensorTypeReference ---

    [Fact]
    public void TensorTypeReference_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Tensor([2L, 3L], TypeFactory.I32);
        Assert.Null(type.Syntax);
    }

    [Theory]
    [InlineData("tensor<2x3xi32>")]
    public void TensorTypeReference_PrintsCorrectlyWithoutSyntax(string expected)
    {
        var type = TypeFactory.Tensor([2L, 3L], TypeFactory.I32);
        Assert.Equal(expected, Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void TensorTypeReference_DynamicDimension_PrintsCorrectly()
    {
        var type = TypeFactory.Tensor([null, 3L], TypeFactory.I32);
        Assert.Equal("tensor<?x3xi32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void TensorTypeReference_UnrankedTensor_PrintsCorrectly()
    {
        var type = TypeFactory.UnrankedTensor(TypeFactory.I32);
        Assert.Equal("tensor<*xi32>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- VectorTypeReference ---

    [Fact]
    public void VectorTypeReference_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.Vector([4L], TypeFactory.F32);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void VectorTypeReference_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.Vector([4L], TypeFactory.F32);
        Assert.Equal("vector<4xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void VectorTypeReference_MultidimensionalVector_PrintsCorrectly()
    {
        var type = TypeFactory.Vector([2L, 8L], TypeFactory.F32);
        Assert.Equal("vector<2x8xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    // --- MemRefTypeReference ---

    [Fact]
    public void MemRefTypeReference_SyntaxIsNullWhenConstructedProgrammatically()
    {
        var type = TypeFactory.MemRef([10L], TypeFactory.F32);
        Assert.Null(type.Syntax);
    }

    [Fact]
    public void MemRefTypeReference_PrintsCorrectlyWithoutSyntax()
    {
        var type = TypeFactory.MemRef([10L], TypeFactory.F32);
        Assert.Equal("memref<10xf32>", Printer.PrintType(type, ReplaceOptions));
    }

    [Fact]
    public void MemRefTypeReference_UnrankedMemRef_PrintsCorrectly()
    {
        var type = TypeFactory.UnrankedMemRef(TypeFactory.I32);
        Assert.Equal("memref<*xi32>", Printer.PrintType(type, ReplaceOptions));
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

    // --- TypeDefinition assembly formats are wired up ---

    [Fact]
    public void AllCollectionTypeDefinitionsHaveAssemblyFormats()
    {
        Assert.NotNull(FunctionTypeReference.TypeDefinition.AssemblyFormat);
        Assert.NotNull(TupleTypeReference.TypeDefinition.AssemblyFormat);
        Assert.NotNull(TensorTypeReference.TypeDefinition.AssemblyFormat);
        Assert.NotNull(VectorTypeReference.TypeDefinition.AssemblyFormat);
        Assert.NotNull(MemRefTypeReference.TypeDefinition.AssemblyFormat);
    }
}
