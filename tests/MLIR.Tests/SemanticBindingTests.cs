namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Dialects.Builtin;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Semantics.Types.Collections;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using Xunit;

public sealed partial class SemanticTests
{
    [Fact]
    public void BindsRegisteredOperationsToDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = module.Operations[0];
        Assert.True(operation.IsKnown);
        Assert.IsType<GenericOperation>(operation);
        Assert.Equal("arith.addi", operation.Name);
        Assert.Equal("\"arith.addi\"", operation.Syntax?.Name);
        Assert.Equal("arith", operation.DialectName);
        Assert.NotNull(operation.Definition);
        Assert.Equal("%0", operation.Results[0].Name);
        Assert.Equal("%lhs", operation.OperandValues[0]?.Name);
    }

    [Fact]
    public void BindsBuiltinTypesWithoutRegistry()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() : (tensor<2x?xf32>, index) -> tuple<vector<4xf32>, memref<*xf32, #map>>"));

        var function = Assert.IsType<FunctionTypeReference>(module.Operations[0].TypeSignatureReference);
        var tensor = Assert.IsType<TensorTypeReference>(function.Inputs[0]);
        var index = Assert.IsType<IndexType>(function.Inputs[1]);
        var tuple = Assert.IsType<TupleTypeReference>(function.Results[0]);
        var vector = Assert.IsType<VectorTypeReference>(tuple.Elements[0]);
        var memref = Assert.IsType<MemRefTypeReference>(tuple.Elements[1]);

        Assert.Equal(new long?[] { 2, null }, tensor.Dimensions);
        Assert.IsType<UnknownTypeReference>(tensor.ElementType);
        Assert.Equal("index", index.Name);
        Assert.Equal(new long?[] { 4 }, vector.Dimensions);
        Assert.True(memref.IsUnranked);
        Assert.Equal("#map", Assert.Single(memref.TrailingParameters).Text);
    }

    [Fact]
    public void TypeReferencesCompareBySemanticIdentityInsteadOfSyntaxIdentity()
    {
        var left = Binder.BindModule(Parser.ParseModule("\"test.op\"() : tensor<2x?xf32>")).Operations[0].TypeSignatureReference!;
        var right = Binder.BindModule(Parser.ParseModule("\"test.op\"() : tensor<2x?xf32>")).Operations[0].TypeSignatureReference!;
        var different = Binder.BindModule(Parser.ParseModule("\"test.op\"() : tensor<3x?xf32>")).Operations[0].TypeSignatureReference!;

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(left, different);
    }

    [Fact]
    public void BuiltinRegisteredTypeWrappersCompareEqualToBuiltinSemanticTypes()
    {
        // Register the builtin dialect using the correct "builtin.integer" canonical name
        // so that the binder's GetStructuredTypeDefinitionName lookup succeeds.
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [],
                [new TypeDefinition("builtin.integer", new BuiltinIntegerTypeAssemblyFormat())]));

        var builtin = Binder.BindModule(Parser.ParseModule("\"test.op\"() : i32")).Operations[0].TypeSignatureReference!;
        var registered = Binder.BindModule(Parser.ParseModule("\"test.op\"() : i32", registry), registry).Operations[0].TypeSignatureReference!;

        Assert.IsType<IntegerType>(builtin);
        Assert.IsType<IntegerType>(registered);
        Assert.Equal(builtin, registered);
        Assert.Equal(registered, builtin);
    }

    [Fact]
    public void BindsScalarFloatTypesToGeneratedTypeDefsWhenRegistered()
    {
        // Verify that when the builtin dialect is registered, scalar float syntax resolves
        // to the generated float TypeDef subclasses (not FloatTypeReference base) with
        // their canonical TypeDefinitions.
        var registry = new DialectRegistry();
        registry.RegisterDialect(BuiltinDialectRegistration.Create());

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() : (f32, bf16, f16, f64) -> ()"),
            registry);
        var function = Assert.IsType<FunctionTypeReference>(module.Operations[0].TypeSignatureReference);

        var f32 = Assert.IsType<Float32Type>(function.Inputs[0]);
        var bf16 = Assert.IsType<BFloat16Type>(function.Inputs[1]);
        var f16 = Assert.IsType<Float16Type>(function.Inputs[2]);
        var f64 = Assert.IsType<Float64Type>(function.Inputs[3]);

        Assert.Equal("f32", f32.Name);
        Assert.Equal("bf16", bf16.Name);
        Assert.Equal("f16", f16.Name);
        Assert.Equal("f64", f64.Name);

        Assert.Same(Float32Type.TypeDefinition, f32.Definition);
        Assert.Same(BFloat16Type.TypeDefinition, bf16.Definition);
        Assert.Same(Float16Type.TypeDefinition, f16.Definition);
        Assert.Same(Float64Type.TypeDefinition, f64.Definition);
    }

    [Fact]
    public void BindsIndexAndNoneToGeneratedTypeDefsWhenRegistered()
    {
        // Verify that when the builtin dialect is registered, index and none syntax resolves
        // to generated TypeDef instances with their canonical TypeDefinitions.
        var registry = new DialectRegistry();
        registry.RegisterDialect(BuiltinDialectRegistration.Create());

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() : (index, none) -> ()"),
            registry);
        var function = Assert.IsType<FunctionTypeReference>(module.Operations[0].TypeSignatureReference);

        var index = Assert.IsType<IndexType>(function.Inputs[0]);
        var none = Assert.IsType<NoneType>(function.Inputs[1]);

        Assert.Same(IndexType.TypeDefinition, index.Definition);
        Assert.Same(NoneType.TypeDefinition, none.Definition);
    }

    [Fact]
    public void GeneratedScalarTypeDefinitionsUseAssemblyFormatNotFactory()
    {
        // Generated scalar TypeDefinitions must use assembly format (not factory delegate)
        // so that scalar binding goes through ITypeAssemblyFormat.Bind.
        Assert.NotNull(IntegerType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float32Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(BFloat16Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float16Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float64Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float80Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(Float128Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(FloatTF32Type.TypeDefinition.AssemblyFormat);
        Assert.NotNull(IndexType.TypeDefinition.AssemblyFormat);
        Assert.NotNull(NoneType.TypeDefinition.AssemblyFormat);
    }

    [Fact]
    public void RegisteredTypeDefinitionWithoutAssemblyFormatBindsToUnknownTypeReferenceWithDefinitionSet()
    {
        // A TypeDefinition registered without an assembly format should produce an
        // UnknownTypeReference whose Definition is non-null. The binder has no factory-delegate
        // fallback; the definition metadata is preserved so callers can still identify the family.
        var definition = new TypeDefinition("test.plain");
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("test", [], [], [definition]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.op\"() : !test.plain", registry),
            registry);

        var type = module.Operations[0].TypeSignatureReference;
        var unknown = Assert.IsType<UnknownTypeReference>(type);
        Assert.Equal("test.plain", unknown.Name);
        Assert.Same(definition, unknown.Definition);
    }

    [Fact]
    public void UnknownTypeReferencesDoNotCompareEqualToKnownTypesWithTheSameName()
    {
        var unknown = new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null);
        var known = Binder.BindModule(Parser.ParseModule("\"test.op\"() : i32")).Operations[0].TypeSignatureReference!;

        Assert.NotEqual(unknown, known);
        Assert.NotEqual(known, unknown);
    }

    [Fact]
    public void TypesFactoryCreatesBuiltinSemanticTypesErgonomically()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(BuiltinDialectRegistration.Create());

        var function = TypeFactory.Function(
            [TypeFactory.Tensor([2, null], TypeFactory.F32), TypeFactory.Index],
            [TypeFactory.Tuple(TypeFactory.Vector([4], TypeFactory.F32), TypeFactory.UnrankedMemRef(TypeFactory.F32, "#map"))]);

        var rebound = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() : (tensor<2x?xf32>, index) -> tuple<vector<4xf32>, memref<*xf32, #map>>", registry),
            registry)
            .Operations[0]
            .TypeSignatureReference!;

        Assert.Equal(rebound, function);
    }

    [Fact]
    public void TypeFactoryFloatPropertiesReturnGeneratedBuiltinFloatClasses()
    {
        // TypeFactory.F16/F32/F64/BF16/TF32 must now return generated concrete classes.
        Assert.IsType<Float16Type>(TypeFactory.F16);
        Assert.IsType<Float32Type>(TypeFactory.F32);
        Assert.IsType<Float64Type>(TypeFactory.F64);
        Assert.IsType<BFloat16Type>(TypeFactory.BF16);
        Assert.IsType<FloatTF32Type>(TypeFactory.TF32);
    }

    [Fact]
    public void TypeFactoryFloatPropertiesCarryCorrectNames()
    {
        Assert.Equal("f16", TypeFactory.F16.Name);
        Assert.Equal("f32", TypeFactory.F32.Name);
        Assert.Equal("f64", TypeFactory.F64.Name);
        Assert.Equal("bf16", TypeFactory.BF16.Name);
        Assert.Equal("tf32", TypeFactory.TF32.Name);
    }

    [Fact]
    public void FloatSyntaxWithoutRegistryProducesUnknownTypeReferenceWithCanonicalName()
    {
        // Without a dialect registry, canonical float spellings produce UnknownTypeReference
        // rather than a typed concrete class. Callers that need typed float references must
        // register the builtin dialect.
        var module = Binder.BindModule(Parser.ParseModule("\"test.op\"() : (f32, bf16, tf32) -> ()"));
        var function = Assert.IsType<FunctionTypeReference>(module.Operations[0].TypeSignatureReference);

        var f32 = Assert.IsType<UnknownTypeReference>(function.Inputs[0]);
        var bf16 = Assert.IsType<UnknownTypeReference>(function.Inputs[1]);
        var tf32 = Assert.IsType<UnknownTypeReference>(function.Inputs[2]);

        Assert.Equal("builtin.f32", f32.Name);
        Assert.Equal("builtin.bf16", bf16.Name);
        Assert.Equal("builtin.tf32", tf32.Name);

        Assert.Null(f32.Definition);
        Assert.Null(bf16.Definition);
        Assert.Null(tf32.Definition);
    }

    [Fact]
    public void BindsAllCanonicalFloatMnemonicsWhenRegistered()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(BuiltinDialectRegistration.Create());

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() : (f16, f32, f64, bf16, tf32) -> ()"),
            registry);
        var function = Assert.IsType<FunctionTypeReference>(module.Operations[0].TypeSignatureReference);

        Assert.IsType<Float16Type>(function.Inputs[0]);
        Assert.IsType<Float32Type>(function.Inputs[1]);
        Assert.IsType<Float64Type>(function.Inputs[2]);
        Assert.IsType<BFloat16Type>(function.Inputs[3]);
        Assert.IsType<FloatTF32Type>(function.Inputs[4]);
    }

    [Fact]
    public void LeavesUnknownOperationsUnbound()
    {
        var module = Binder.BindModule(Parser.ParseModule("\"test.unknown\"() : () -> ()"));

        var operation = module.Operations[0];
        Assert.False(operation.IsKnown);
        Assert.IsType<GenericOperation>(operation);
        Assert.Null(operation.Definition);
        Assert.Equal("test.unknown", operation.Name);
    }

    [Fact]
    public void BinderCanConstructGeneratedTypedOperations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.addi",
                        operation => operation
                            .Operand("lhs")
                            .Operand("rhs")
                            .Result("result")
                            .WithFactory(static context => new GeneratedAddIOperation(context)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = Assert.IsType<GeneratedAddIOperation>(module.Operations[0]);
        Assert.Equal("%lhs", operation.LeftOperand.Name);
        Assert.Equal("%rhs", operation.RightOperand.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact]
    public void BindsNestedRegionsBlocksArgumentsAndAttributes()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0(%arg0: i32):\n" +
                "    \"func.return\"(%arg0) {value = 1 : i32} : (i32) -> ()\n" +
                "} : (i1) -> ()"));

        var region = module.Operations[0].Regions[0];
        var block = region.Blocks[0];
        var nestedOperation = block.Operations[0];

        Assert.Single(module.Operations);
        Assert.Equal("^bb0", block.Label);
        Assert.Single(block.Arguments);
        Assert.Equal("%arg0", block.Arguments[0].Name);
        Assert.Single(nestedOperation.Attributes);
        Assert.Equal("value", nestedOperation.Attributes[0].Name);
        Assert.Equal("1 : i32", nestedOperation.Attributes[0].Value.Syntax!.ToString());
        Assert.Equal("i32", block.Arguments[0].Type.Name);
    }

    [Fact]
    public void BindsAttributeAndTypeDefinitionsFromTheRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [new AttributeDefinition("dense", new DenseAttributeAssemblyFormat())],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat())]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.op\"() {value = #dense<[1, 2]> : tensor<2xi32>} : i32", registry),
            registry);

        var operation = module.Operations[0];

        Assert.True(operation.Attributes[0].Value.IsKnown);
        Assert.Equal("dense", operation.Attributes[0].Value.Name);
        Assert.Equal("dense", Assert.IsType<DenseAttributeValue>(operation.Attributes[0].Value).Kind);
        Assert.IsType<DenseAttributeValueSyntax>(operation.Attributes[0].Value.Syntax);
        Assert.NotNull(operation.TypeSignatureReference);
        Assert.True(operation.TypeSignatureReference!.IsKnown);
        Assert.Equal("i32", operation.TypeSignatureReference.Name);
        Assert.Equal(32, Assert.IsType<IntegerType>(operation.TypeSignatureReference).Width);
        Assert.IsType<global::MLIR.Syntax.Types.Primitives.BuiltinIntegerTypeSyntax>(operation.TypeSignatureReference.Syntax);
    }

    [Fact]
    public void OperationAssemblyFormatCanParseAttributesUsingExpectedDefinition()
    {
        var registry = new DialectRegistry();
        var i32AttributeDefinition = new AttributeDefinition("i32", new I32AttributeAssemblyFormat());
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [i32AttributeDefinition],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat())]));
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .RequiredAttribute("value")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(i32AttributeDefinition)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = arith.constant 42 : i32", registry),
            registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<I32AttributeValue>(operation.ValueAttribute.Value);
        Assert.Equal(42, value.Value);
        Assert.IsType<IntegerLiteralAttributeSyntax>(value.Syntax);
        Assert.Equal("%0 = arith.constant 42 : i32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Fact]
    public void OperationAssemblyFormatCanBindF32AttributesAsSinglePrecisionValues()
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new TestF32AttributeAssemblyFormat());
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .Result("result")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(f32AttributeDefinition)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = arith.constant 1.500 : f32", registry),
            registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<TestF32AttributeValue>(operation.ValueAttribute.Value);
        Assert.Equal(1.5f, value.Value);
        Assert.IsType<FloatingPointAttributeValueSyntax>(value.Syntax);
    }

    [Fact]
    public void OperationAssemblyFormatCanBindF64AttributesAsDoublePrecisionValues()
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new TestF64AttributeAssemblyFormat());
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .Result("result")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(f64AttributeDefinition)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = arith.constant 1.500 : f64", registry),
            registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<TestF64AttributeValue>(operation.ValueAttribute.Value);
        Assert.Equal(1.5, value.Value);
        Assert.IsType<FloatingPointAttributeValueSyntax>(value.Syntax);
    }

    [Theory]
    [InlineData("+1.500", 1.5f)]
    public void OperationAssemblyFormatCanRoundTripF32Attributes(string sourceValue, float expectedValue)
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new TestF32AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f32AttributeDefinition);

        var source = $"%0 = arith.constant {sourceValue} : f32";
        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<TestF32AttributeValue>(operation.ValueAttribute.Value);

        Assert.Equal(expectedValue, value.Value);
        Assert.IsType<FloatingPointAttributeValueSyntax>(value.Syntax);
        Assert.Equal($"%0 = arith.constant {FloatingPointLiteralParser.FormatSingle(value.Value)} : f32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Theory]
    [InlineData("+2.5000", 2.5)]
    [InlineData("-3.125e200", -3.125e200)]
    public void OperationAssemblyFormatCanRoundTripF64Attributes(string sourceValue, double expectedValue)
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new TestF64AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f64AttributeDefinition);

        var source = $"%0 = arith.constant {sourceValue} : f64";
        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<TestF64AttributeValue>(operation.ValueAttribute.Value);

        Assert.Equal(expectedValue, value.Value);
        Assert.IsType<FloatingPointAttributeValueSyntax>(value.Syntax);
        Assert.Equal($"%0 = arith.constant {FloatingPointLiteralParser.FormatDouble(value.Value)} : f64", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Theory]
    [InlineData("1.", 1f)]
    [InlineData("1.e3", 1000f)]
    [InlineData("+1.", 1f)]
    public void OperationAssemblyFormatCanParseAdditionalF32FloatForms(string sourceValue, float expectedValue)
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new TestF32AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f32AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f32", registry),
            registry);

        var value = Assert.IsType<TestF32AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value);
        Assert.Equal(expectedValue, value.Value);
    }

    [Theory]
    [InlineData("0x3f800000", "finite", 1f)]
    [InlineData("0x7f800000", "posinf", 0f)]
    [InlineData("-inf", "neginf", 0f)]
    [InlineData("nan", "nan", 0f)]
    [InlineData("0x7fc00000", "nan", 0f)]
    public void OperationAssemblyFormatCanBindMoreF32FloatForms(string sourceValue, string kind, float expectedValue)
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new TestF32AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f32AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f32", registry),
            registry);

        var value = Assert.IsType<TestF32AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value);

        switch (kind)
        {
            case "finite":
                Assert.Equal(expectedValue, value.Value);
                break;
            case "posinf":
                Assert.True(float.IsPositiveInfinity(value.Value));
                break;
            case "neginf":
                Assert.True(float.IsNegativeInfinity(value.Value));
                break;
            case "nan":
                Assert.True(float.IsNaN(value.Value));
                break;
        }
    }

    [Theory]
    [InlineData("1.", 1d)]
    [InlineData("1.e+3", 1000d)]
    [InlineData("+1.", 1d)]
    public void OperationAssemblyFormatCanParseAdditionalF64FloatForms(string sourceValue, double expectedValue)
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new TestF64AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f64AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f64", registry),
            registry);

        var value = Assert.IsType<TestF64AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value);
        Assert.Equal(expectedValue, value.Value);
    }

    [Theory]
    [InlineData("0x3ff0000000000000", "finite", 1d)]
    [InlineData("0x7ff0000000000000", "posinf", 0d)]
    [InlineData("-inf", "neginf", 0d)]
    [InlineData("nan", "nan", 0d)]
    [InlineData("0x7ff8000000000000", "nan", 0d)]
    public void OperationAssemblyFormatCanBindMoreF64FloatForms(string sourceValue, string kind, double expectedValue)
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new TestF64AttributeAssemblyFormat());
        var registry = CreateFloatingPointConstantRegistry(f64AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f64", registry),
            registry);

        var value = Assert.IsType<TestF64AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value);

        switch (kind)
        {
            case "finite":
                Assert.Equal(expectedValue, value.Value);
                break;
            case "posinf":
                Assert.True(double.IsPositiveInfinity(value.Value));
                break;
            case "neginf":
                Assert.True(double.IsNegativeInfinity(value.Value));
                break;
            case "nan":
                Assert.True(double.IsNaN(value.Value));
                break;
        }
    }

    [Fact]
    public void BindsTypedSuccessorReferences()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"cf.cond_br\"(%cond) [^then, ^else] : (i1) -> ()"));

        var operation = module.Operations[0];

        Assert.Equal("^then", operation.Successors[0].Label);
        Assert.Equal("^else", operation.Successors[1].Label);
    }

    [Fact]
    public void BindsSuccessorsToBlockInstances()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"func.func\"() {\n" +
                "^bb0:\n" +
                "  \"cf.br\"() [^bb1] : () -> ()\n" +
                "^bb1:\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : () -> ()"));

        var region = module.Operations[0].Regions[0];
        var branchOp = region.Blocks[0].Operations[0];
        var bb1 = region.Blocks[1];

        Assert.Same(bb1, branchOp.Successors[0].Block);
        Assert.Equal("^bb1", branchOp.Successors[0].Label);
    }

    [Fact]
    public void BindsFuncOpBlockArgumentNames()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"func.func\"() {\n" +
                "^entry(%lhs: i64, %rhs: i32):\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : () -> ()"));

        var block = module.Operations[0].Regions[0].Blocks[0];

        Assert.Equal(["%lhs", "%rhs"], block.Arguments.Select(static argument => argument.Name).ToArray());
    }

    [Fact]
    public void BlockTracksItsSuccessorUses()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"func.func\"() {\n" +
                "^bb0:\n" +
                "  \"cf.br\"() [^bb1] : () -> ()\n" +
                "^bb1:\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : () -> ()"));

        var region = module.Operations[0].Regions[0];
        var branchOp = region.Blocks[0].Operations[0];
        var bb1 = region.Blocks[1];

        Assert.Single(bb1.Uses);
        Assert.Same(branchOp.Successors[0], bb1.Uses[0]);
    }

    [Fact]
    public void DocumentBindUsesTheDialectRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("func", [new OperationDefinition("func.return")]));

        var module = Document.Parse("\"func.return\"() : () -> ()").Bind(registry);

        Assert.True(module.Operations[0].IsKnown);
        Assert.Equal("func.return", module.Operations[0].Name);
    }

    [Fact]
    public void OperationCanCheckForAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var operation = module.Operations[0];

        Assert.True(operation.HasAttribute("value"));
        Assert.False(operation.HasAttribute("fastmath"));
    }

    [Fact]
    public void OperationCanRetrieveAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var attribute = module.Operations[0].GetAttribute("value");

        Assert.Equal("value", attribute.Name);
        Assert.Equal("0 : i32", attribute.Value.Syntax!.ToString());
    }

    [Fact]
    public void OperationViewProvidesTypedWrapperOverSemanticOperation()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(CreateArithConstantDialect());

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);

        var view = new ArithConstantView(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]));

        Assert.Equal("%0", view.Results[0]);
        Assert.Equal("%0", view.ResultValue.Name);
        Assert.Equal("0 : i32", view.ValueAttribute.Value.Syntax!.ToString());
    }

    [Fact]
    public void OperationViewRejectsUnexpectedOperationNames()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"func.return\"() : () -> ()"));

        var exception = Assert.Throws<ArgumentException>(() => new ArithConstantView(module.Operations[0]));

        Assert.Contains("arith.constant", exception.Message);
        Assert.Contains("func.return", exception.Message);
    }

    [Fact]
    public void SemanticReferencesExposeSourceLocations()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) [^bb1] : (i32, i32) -> i32"));

        var operation = module.Operations[0];

        Assert.Equal(1, operation.Location.Line);
        // Location now spans from the first result token (%0) to the end of the type signature,
        // so the column is 1 (the start of %0) rather than 6 (the start of "arith.addi").
        Assert.Equal(1, operation.Location.Column);
        Assert.Equal(1, operation.Results[0].Location.Line);
        Assert.Equal(1, operation.Results[0].Location.Column);
        Assert.Equal(1, operation.OperandValues[0]?.Location.Line);
        Assert.Equal(19, operation.OperandValues[0]?.Location.Column);
        Assert.Equal(1, operation.Successors[0].Location.Line);
        Assert.Equal(32, operation.Successors[0].Location.Column);
    }

    [Fact]
    public void BinderPassesTypedAttributeSelfTypeToAttributeAssemblyFormatBind()
    {
        AttributeValueConstructionContext? capturedContext = null;
        var valueConstraint = new AttributeConstraintDefinition(
            "test.int",
            new CapturingIntegerAttributeAssemblyFormat(context => capturedContext = context));
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("test", [new OperationDefinition("test.op", attributeDefinitions: [new OperationAttributeDefinition("value", true, valueConstraint)])]));

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() {value = 42 : i32} : () -> ()"),
            registry);

        var valueAttribute = module.Operations[0].GetAttribute("value");

        Assert.NotNull(capturedContext);
        Assert.IsType<IntegerAttributeValueSyntax>(capturedContext!.Syntax);
        var selfType = Assert.IsType<IntegerType>(capturedContext.SelfType);
        Assert.Equal(32, selfType.Width);
        Assert.Equal("42", valueAttribute.Value.Syntax!.ToString());
    }
}
