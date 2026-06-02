namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Attributes;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Dialects.Builtin;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;

/// <summary>
/// Binds generic MLIR concrete syntax to semantic nodes using a dialect registry.
/// </summary>
public sealed class Binder
{
    internal Binder(DialectRegistry? dialectRegistry)
    {
        this.dialectRegistry = dialectRegistry;
    }

    private readonly List<AssemblyDiagnostic> diagnostics = [];
    private readonly DialectRegistry? dialectRegistry;
    private readonly Stack<Dictionary<string, Value>> valueScopes = new();
    private readonly Stack<TypeReference?> attributeSelfTypeReferences = new();

    /// <summary>
    /// Reports a binding diagnostic for the current operation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    public void Report(AssemblyDiagnostic diagnostic)
    {
        diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Binds a module syntax tree to a semantic module.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="dialectRegistry">The dialect registry used to resolve known operations.</param>
    /// <returns>The semantic module.</returns>
    public static Module BindModule(ModuleSyntax syntax, DialectRegistry? dialectRegistry = null)
    {
        var operations = new List<Operation>();
        var binder = new Binder(dialectRegistry);
        binder.PushValueScope();
        foreach (var operation in syntax.Operations)
        {
            var boundOperation = binder.BindOperation(operation);
            operations.Add(boundOperation);
            foreach (var result in boundOperation.Results)
            {
                binder.DefineValue(result);
            }
        }

        binder.PopValueScope();
        return new Module(syntax, operations, binder.diagnostics);
    }

    /// <summary>
    /// Binds an operation syntax tree to a semantic operation, recursively binding any nested regions and blocks.
    /// If the operation's name matches a known operation in the dialect registry,
    /// the corresponding definition will be used to construct a typed operation; otherwise, an <see cref="GenericOperation"/> will be created.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic operation.</returns>
    public Operation BindOperation(OperationSyntax syntax)
        => BindOperation(syntax, null);

    internal Operation BindOperation(OperationSyntax syntax, IReadOnlyDictionary<string, Block>? blocksByLabel)
    {
        var name = NormalizeOperationName(syntax.Name);
        OperationDefinition? definition = null;
        if (dialectRegistry != null)
        {
            dialectRegistry.TryGetOperationForParsing(name, out definition);
        }

        if (syntax.Body is GenericOperationBodySyntax genericBody)
        {
            return BindGenericOperation(syntax, genericBody, name, definition, blocksByLabel);
        }
        else if (definition != null && definition.AssemblyFormat != null)
        {
            try
            {
                return definition.AssemblyFormat.Bind(syntax, this);
            }
            catch (Exception ex)
            {
                Report(new AssemblyDiagnostic(syntax.Location, $"Failed to bind custom assembly for operation '{name}': {ex.Message}"));
                return new UninterpretedOperation(syntax, name);
            }
        }
        else
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Unrecognized operation '{name}' with no generic body and no assembly format defined."));
            return new UninterpretedOperation(syntax, name);
        }
    }

    internal Operation BindGenericOperation(OperationSyntax syntax, GenericOperationBodySyntax body, string name, OperationDefinition? definition, IReadOnlyDictionary<string, Block>? blocksByLabel = null)
    {
        var regions = new List<Region>();
        foreach (var region in body.Regions)
        {
            regions.Add(BindRegion(region));
        }

        var attributeList = new List<NamedAttribute>();
        foreach (var attribute in body.Attributes)
        {
            attributeList.Add(BindNamedAttribute(attribute, definition));
        }

        var attributes = new NamedAttributeCollection(attributeList);

        TypeReference? typeSignatureReference = null;
        if (body.TypeSignatureSyntax != null)
        {
            typeSignatureReference = BindTypeReference(body.TypeSignatureSyntax);
        }

        var resultValues = BindOperationResults(syntax.ResultList);
        var operandValues = BindValueUses(body.OperandList.Items);
        var successorTokens = body.SuccessorList.Items;

        // Resolve successor label tokens to Block instances using the current region's label map.
        var successorBlocks = new Block?[successorTokens.Count];
        for (var i = 0; i < successorTokens.Count; i++)
        {
            blocksByLabel?.TryGetValue(successorTokens[i].Text, out successorBlocks[i]);
        }

        Operation operation;
        if (definition != null && CheckGenericOperationConstraints(syntax, definition, regions, attributes, typeSignatureReference, resultValues, operandValues, successorTokens))
        {
            var constructionContext = new OperationConstructionContext(
                syntax,
                name,
                definition,
                regions,
                attributes,
                typeSignatureReference,
                resultValues,
                operandValues,
                successorBlocks);
            operation = definition.Factory(constructionContext);
        }
        else
        {
            operation = new GenericOperation(
                syntax,
                name,
                definition,
                regions,
                attributes,
                typeSignatureReference,
                resultValues,
                operandValues,
                successorBlocks);
        }

        return operation;
    }

    private bool CheckGenericOperationConstraints(
        OperationSyntax syntax,
        OperationDefinition definition,
        IReadOnlyList<Region> regions,
        NamedAttributeCollection attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<OperationResult> resultValues,
        IReadOnlyList<Value> operandValues,
        IReadOnlyList<Token> successorTokens)
    {
        var isValid = true;
        if (definition.RegionCount.HasValue && regions.Count != definition.RegionCount.Value)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected exactly {definition.RegionCount.Value} region(s) but found {regions.Count}."));
            isValid = false;
        }
        else if (regions.Count < definition.RegionMinCount)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected at least {definition.RegionMinCount} region(s) but found {regions.Count}."));
            isValid = false;
        }

        if (definition.ResultCount.HasValue && resultValues.Count != definition.ResultCount.Value)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected exactly {definition.ResultCount.Value} result(s) but found {resultValues.Count}."));
            isValid = false;
        }
        else if (resultValues.Count < definition.ResultMinCount)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected at least {definition.ResultMinCount} result(s) but found {resultValues.Count}."));
            isValid = false;
        }

        if (definition.OperandCount.HasValue && operandValues.Count != definition.OperandCount.Value)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected exactly {definition.OperandCount.Value} operand(s) but found {operandValues.Count}."));
            isValid = false;
        }
        else if (operandValues.Count < definition.OperandMinCount)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected at least {definition.OperandMinCount} operand(s) but found {operandValues.Count}."));
            isValid = false;
        }

        if (definition.SuccessorCount.HasValue && successorTokens.Count != definition.SuccessorCount.Value)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected exactly {definition.SuccessorCount.Value} successor(s) but found {successorTokens.Count}."));
            isValid = false;
        }
        else if (successorTokens.Count < definition.SuccessorMinCount)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected at least {definition.SuccessorMinCount} successor(s) but found {successorTokens.Count}."));
            isValid = false;
        }

        foreach (var attributeDefinition in definition.AttributeDefinitions)
        {
            if (attributeDefinition.IsRequired && !attributes.Any(a => a.Name == attributeDefinition.Name))
            {
                Report(new AssemblyDiagnostic(syntax.Location, $"{definition.Name} expects a '{attributeDefinition.Name}' required attribute."));
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// Binds a region syntax tree to a semantic region, recursively binding any nested blocks.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic region.</returns>
    public Region BindRegion(RegionSyntax syntax)
    {
        // Phase 1: create all Block objects (with arguments, no operations yet) so every
        // block label is known before any operation's successors are resolved.
        // No value scope is needed in this phase: BindTypeReference resolves types through the
        // type registry, not through SSA value lookups. The binder value-scope stack is only
        // consulted when resolving operand SSA names, which happens in Phase 2.
        var blocks = new List<Block>(syntax.Blocks.Count);
        var blocksByLabel = new Dictionary<string, Block>(syntax.Blocks.Count);
        foreach (var blockSyntax in syntax.Blocks)
        {
            var arguments = new List<BlockArgument>(blockSyntax.Arguments.Count);
            foreach (var argument in blockSyntax.Arguments)
            {
                arguments.Add(new BlockArgument(argument, BindTypeReference(argument.TypeSyntax)));
            }

            var block = new Block(blockSyntax, arguments, []);
            blocks.Add(block);
            blocksByLabel[block.Label] = block;
        }

        // Phase 2: bind each block's operations now that the full label map is available.
        // Successor tokens are resolved to Block instances on the fly, so no post-pass is needed.
        for (var b = 0; b < syntax.Blocks.Count; b++)
        {
            var blockSyntax = syntax.Blocks[b];
            var block = blocks[b];

            PushValueScope();

            foreach (var argument in block.Arguments)
            {
                DefineValue(argument);
            }

            foreach (var operation in blockSyntax.Operations)
            {
                var boundOperation = BindOperation(operation, blocksByLabel);
                block.AddOperationFromSyntax(boundOperation);
                foreach (var result in boundOperation.Results)
                {
                    DefineValue(result);
                }
            }

            PopValueScope();
        }

        return new Region(syntax, blocks);
    }

    /// <summary>
    /// Binds a block syntax tree to a semantic block, recursively binding any nested operations.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic block.</returns>
    public Block BindBlock(BlockSyntax syntax)
    {
        PushValueScope();

        var arguments = new List<BlockArgument>();
        foreach (var argument in syntax.Arguments)
        {
            var blockArgument = new BlockArgument(argument, BindTypeReference(argument.TypeSyntax));
            arguments.Add(blockArgument);
            DefineValue(blockArgument);
        }

        var operations = new List<Operation>();
        foreach (var operation in syntax.Operations)
        {
            var boundOperation = BindOperation(operation);
            operations.Add(boundOperation);
            foreach (var result in boundOperation.Results)
            {
                DefineValue(result);
            }
        }

        PopValueScope();
        return new Block(syntax, arguments, operations);
    }

    private static string NormalizeOperationName(string name)
    {
        if (name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"')
        {
            return name.Substring(1, name.Length - 2);
        }

        return name;
    }

    /// <summary>
    /// Binds a syntax token to a semantic SSA value use, resolving it to the nearest visible definition when possible.
    /// </summary>
    /// <param name="token">The syntax token to bind.</param>
    /// <returns>The semantic value.</returns>
    public Value BindValueReference(Token token)
    {
        if (TryLookupValue(token.Text, out var value))
        {
            return value;
        }

        return new UnresolvedValue(token);
    }

    /// <summary>
    /// Creates semantic SSA value uses for the given tokens.
    /// </summary>
    /// <param name="tokens">The tokens for which to create values.</param>
    /// <returns>The list of semantic values.</returns>
    public IReadOnlyList<Value> BindValueUses(IReadOnlyList<Token> tokens)
    {
        var values = new List<Value>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(BindValueReference(token));
        }

        return values;
    }

    /// <summary>
    /// Creates operation result definitions for the given result tokens.
    /// </summary>
    /// <param name="tokens">The result tokens.</param>
    /// <returns>The unbound operation results.</returns>
    public IReadOnlyList<OperationResult> BindOperationResults(IReadOnlyList<Token> tokens)
    {
        var values = new List<OperationResult>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(new OperationResult(token));
        }

        return values;
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(AttributeValueSyntax syntax)
    {
        return BindAttributeValue(syntax, (AttributeConstraintDefinition?)null);
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value, preferring the supplied expected attribute definition when known.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="expectedDefinitionName">The expected parser-facing attribute definition name, if one is known.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(AttributeValueSyntax syntax, string? expectedDefinitionName)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return BindAttributeValue(syntax, expectedDefinition);
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value, preferring the supplied expected attribute definition when known.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="expectedDefinition">The expected attribute definition, if one is known.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(AttributeValueSyntax syntax, AttributeConstraintDefinition? expectedDefinition)
    {
        return BindAttributeValueCore(syntax, expectedDefinition);
    }

    private AttributeValue BindAttributeValueCore(AttributeValueSyntax syntaxNode, AttributeConstraintDefinition? expectedDefinition)
    {
        var outerSyntax = syntaxNode;
        var selfTypeReference = TryBindTypedAttributeSelfType(syntaxNode, out var payloadSyntax, out _);
        syntaxNode = payloadSyntax;

        // TODO: don't just turn the syntax node back into text and reparse it; instead, take advantage
        // of the fact that this only applies to custom attributes and grab the name directly from the syntax node.
        var canonicalName = TryGetAttributeDefinitionName(syntaxNode.ToString());
        AttributeConstraintDefinition? definition = null;
        if (expectedDefinition != null)
        {
            definition = expectedDefinition;
        }

        if (definition == null && canonicalName != null && dialectRegistry != null)
        {
            if (dialectRegistry.TryGetAttribute(canonicalName, out var attributeDefinition))
            {
                definition = attributeDefinition;
            }
        }

        var isBuiltinFallbackDefinition = false;
        if (definition == null)
        {
            definition = TryGetBuiltinAttributeDefinition(syntaxNode);
            isBuiltinFallbackDefinition = definition != null;
        }

        AttributeValue attribute;
        if (definition?.AssemblyFormat != null)
        {
            attribute = BindAttributeValueWithAssemblyFormat(
                isBuiltinFallbackDefinition ? outerSyntax : syntaxNode,
                definition,
                selfTypeReference);
        }
        else
        {
            attribute = new UnknownAttributeValue(
                outerSyntax,
                definition?.Name ?? canonicalName,
                definition);
        }

        return attribute;
    }

    /// <summary>
    /// Binds each attribute-value syntax node in a collection through the normal attribute binding path.
    /// </summary>
    /// <param name="items">The syntax nodes to bind.</param>
    /// <returns>The bound semantic attribute values.</returns>
    public IReadOnlyList<AttributeValue> BindAttributeValues(IReadOnlyList<AttributeValueSyntax> items)
    {
        var result = new List<AttributeValue>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            result.Add(BindAttributeValue(items[i], expectedDefinition: null));
        }

        return result;
    }

    /// <summary>
    /// Binds each named attribute syntax node in a collection through the normal attribute binding path.
    /// </summary>
    /// <param name="attributes">The syntax nodes to bind.</param>
    /// <returns>The bound semantic named-attribute collection.</returns>
    public NamedAttributeCollection BindNamedAttributes(IReadOnlyList<NamedAttributeSyntax> attributes)
    {
        var result = new List<NamedAttribute>(attributes.Count);
        for (var i = 0; i < attributes.Count; i++)
        {
            result.Add(BindNamedAttribute(attributes[i]));
        }

        return new NamedAttributeCollection(result);
    }

    private AttributeValue BindAttributeValueWithAssemblyFormat(
        AttributeValueSyntax syntax,
        AttributeConstraintDefinition definition,
        TypeReference? selfTypeReference)
    {
        attributeSelfTypeReferences.Push(selfTypeReference);
        try
        {
            return definition.AssemblyFormat!.Bind(syntax, this);
        }
        finally
        {
            attributeSelfTypeReferences.Pop();
        }
    }

    private TypeReference? CurrentAttributeSelfTypeReference =>
        attributeSelfTypeReferences.Count > 0 ? attributeSelfTypeReferences.Peek() : null;

    private static AttributeConstraintDefinition? TryGetBuiltinAttributeDefinition(AttributeValueSyntax syntax)
    {
        return syntax switch
        {
            BooleanAttributeValueSyntax => BuiltinBooleanAttributeDefinition,
            IntegerAttributeValueSyntax => BuiltinIntegerAttributeDefinition,
            FloatingPointAttributeValueSyntax => BuiltinFloatingPointAttributeDefinition,
            StringAttributeValueSyntax => BuiltinStringAttributeDefinition,
            UnitAttributeValueSyntax or PrefixedUnitAttributeValueSyntax => BuiltinUnitAttributeDefinition,
            TypeAttributeValueSyntax => BuiltinTypeAttributeDefinition,
            SymbolRefAttributeValueSyntax => BuiltinSymbolRefAttributeDefinition,
            OpaqueAttributeValueSyntax => BuiltinOpaqueAttributeDefinition,
            DenseArrayAttributeValueSyntax denseArraySyntax => GetDenseArrayAttributeDefinition(denseArraySyntax),
            ArrayAttributeValueSyntax => BuiltinArrayAttributeDefinition,
            DictionaryAttributeValueSyntax => BuiltinDictionaryAttributeDefinition,
            ElementsAttributeValueSyntax => BuiltinDenseTypedElementsAttributeDefinition,
            _ => null,
        };
    }

    private static AttributeConstraintDefinition GetDenseArrayAttributeDefinition(DenseArrayAttributeValueSyntax syntax)
    {
        var elementTypeText = syntax.ElementTypeSyntax.ToString();
        return elementTypeText switch
        {
            "i1" => BuiltinDenseBooleanArrayAttributeDefinition,
            "i8" => BuiltinDenseI8ArrayAttributeDefinition,
            "i16" => BuiltinDenseI16ArrayAttributeDefinition,
            "i64" => BuiltinDenseI64ArrayAttributeDefinition,
            "f32" => BuiltinDenseF32ArrayAttributeDefinition,
            "f64" => BuiltinDenseF64ArrayAttributeDefinition,
            _ when IsFloatingPointTypeText(elementTypeText) => BuiltinDenseF64ArrayAttributeDefinition,
            _ => BuiltinDenseI32ArrayAttributeDefinition,
        };
    }

    private static bool IsFloatingPointTypeText(string text)
    {
        return text == "f16"
            || text == "bf16"
            || text == "f32"
            || text == "f64"
            || text == "tf32"
            || text == "f80"
            || text == "f128";
    }

    private static readonly AttributeDefinition BuiltinBooleanAttributeDefinition =
        new("builtin.integer", new BooleanLiteralAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinIntegerAttributeDefinition =
        new("builtin.integer", new IntegerLiteralAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinFloatingPointAttributeDefinition =
        new("builtin.float", new FloatingPointLiteralAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinStringAttributeDefinition =
        new("builtin.string", new StringLiteralAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinUnitAttributeDefinition =
        new("builtin.unit", new UnitLiteralAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinTypeAttributeDefinition =
        new("builtin.type", new TypeAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinSymbolRefAttributeDefinition =
        new("builtin.symbol_ref", new BuiltinSymbolRefAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinOpaqueAttributeDefinition =
        new("builtin.opaque", new BuiltinOpaqueAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseI32ArrayAttributeDefinition =
        new("DenseI32ArrayAttr", new DenseIntegerArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseI8ArrayAttributeDefinition =
        new("DenseI8ArrayAttr", new DenseIntegerArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseI16ArrayAttributeDefinition =
        new("DenseI16ArrayAttr", new DenseIntegerArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseI64ArrayAttributeDefinition =
        new("DenseI64ArrayAttr", new DenseIntegerArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseBooleanArrayAttributeDefinition =
        new("DenseBoolArrayAttr", new DenseBooleanArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseF32ArrayAttributeDefinition =
        new("DenseF32ArrayAttr", new DenseFloatingPointArrayAttributeAssemblyFormat("f32"));

    private static readonly AttributeDefinition BuiltinDenseF64ArrayAttributeDefinition =
        new("DenseF64ArrayAttr", new DenseFloatingPointArrayAttributeAssemblyFormat("f64"));

    private static readonly AttributeDefinition BuiltinArrayAttributeDefinition =
        new("builtin.array", new ArrayAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDictionaryAttributeDefinition =
        new("builtin.dictionary", new DictionaryAttributeAssemblyFormat());

    private static readonly AttributeDefinition BuiltinDenseTypedElementsAttributeDefinition =
        new("builtin.dense_typed_elements", new ElementsAttributeAssemblyFormat());

    private TypeReference? TryBindTypedAttributeSelfType(
        AttributeValueSyntax syntax,
        out AttributeValueSyntax payloadSyntax,
        out TypeSyntax? selfTypeSyntax)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            payloadSyntax = typedSyntax.AttributeSyntax;
            selfTypeSyntax = typedSyntax.TypeSyntax;
            return BindTypeReference(typedSyntax.TypeSyntax);
        }

        payloadSyntax = syntax;
        selfTypeSyntax = null;
        return null;
    }

    /// <summary>
    /// Binds a named attribute syntax tree to a semantic named attribute, which includes both the attribute name and value.
    /// </summary>
    /// <param name="syntax">The named attribute syntax tree to bind.</param>
    /// <returns>The semantic named attribute.</returns>
    public NamedAttribute BindNamedAttribute(NamedAttributeSyntax syntax)
    {
        return BindNamedAttribute(syntax, null);
    }

    /// <summary>
    /// Binds a named attribute syntax tree to a semantic named attribute, preferring the operation's
    /// declared attribute constraint when one is available.
    /// </summary>
    public NamedAttribute BindNamedAttribute(NamedAttributeSyntax syntax, OperationDefinition? operationDefinition)
    {
        AttributeConstraintDefinition? expectedConstraint = null;
        if (operationDefinition != null)
        {
            for (var i = 0; i < operationDefinition.AttributeDefinitions.Count; i++)
            {
                var attributeDefinition = operationDefinition.AttributeDefinitions[i];
                if (attributeDefinition.Name == syntax.Name)
                {
                    expectedConstraint = attributeDefinition.ConstraintDefinition;
                    break;
                }
            }
        }

        var value = BindAttributeValue(syntax.ValueSyntax, expectedConstraint);
        return new NamedAttribute(syntax, value);
    }

    /// <summary>
    /// Binds a type reference syntax tree to a semantic type reference.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic type reference.</returns>
    public TypeReference BindTypeReference(TypeSyntax syntax)
    {
        return BindStructuredTypeReference(syntax);
    }

    /// <summary>
    /// Binds a bare type list from a variadic <c>type($operand)</c> directive.
    /// A single type keeps the scalar type-signature shape; multiple
    /// types are represented as a function type whose inputs are the listed types.
    /// </summary>
    public TypeReference BindTypeReferenceList(SeparatedSyntaxList<TypeSyntax> syntax)
    {
        if (syntax.Count == 1)
        {
            return BindTypeReference(syntax[0]);
        }

        return TypeFactory.Function(syntax.Items.Select(BindTypeReference).ToArray(), []);
    }

    private TypeReference BindStructuredTypeReference(TypeSyntax syntax)
    {
        var registeredType = TryBindStructuredTypeDefinition(syntax);
        if (registeredType != null)
        {
            return registeredType;
        }

            switch (syntax)
            {
            case BuiltinIntegerTypeSyntax integerSyntax:
                return new IntegerType(integerSyntax.Width, integerSyntax.Signedness, integerSyntax);
            case BuiltinFloatTypeSyntax floatSyntax:
                // Without a registered dialect, float syntax produces an UnknownTypeReference
                // carrying the canonical name. Canonical float spellings always bind to generated
                // classes when the builtin dialect is registered.
                return new UnknownTypeReference(floatSyntax, "builtin." + floatSyntax.Name, null);
            case BuiltinIndexTypeSyntax indexSyntax:
                return new IndexType(indexSyntax);
            case BuiltinNoneTypeSyntax noneSyntax:
                return new NoneType(noneSyntax);
            case TupleTypeSyntax tupleSyntax:
                return new TupleType(tupleSyntax.Elements.Select(BindTypeReference).ToArray(), tupleSyntax);
            case FunctionTypeSyntax functionSyntax:
                return new FunctionType(
                    functionSyntax.InputTypes.Items.Select(BindTypeReference).ToArray(),
                    GetFunctionResults(functionSyntax).Select(BindTypeReference).ToArray(),
                    functionSyntax);
            case ProjectedToTypeSyntax projectedToSyntax:
                return new FunctionType(
                    [BindTypeReference(projectedToSyntax.SourceType)],
                    [BindTypeReference(projectedToSyntax.ResultType)],
                    projectedToSyntax);
            case TensorTypeSyntax tensorSyntax:
                return tensorSyntax.IsUnranked
                    ? new UnrankedTensorType(
                        BindTypeReference(tensorSyntax.ElementType),
                        tensorSyntax)
                    : new RankedTensorType(
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeShape(tensorSyntax.Dimensions),
                        BindTypeReference(tensorSyntax.ElementType),
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(tensorSyntax.TrailingParameters, 0)!,
                        tensorSyntax);
            case VectorTypeSyntax vectorSyntax:
                return new VectorType(
                    MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeShape(vectorSyntax.Dimensions),
                    BindTypeReference(vectorSyntax.ElementType),
                    null!,
                    vectorSyntax);
            case MemRefTypeSyntax memRefSyntax:
                return memRefSyntax.IsUnranked
                    ? new UnrankedMemRefType(
                        BindTypeReference(memRefSyntax.ElementType),
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 0)!,
                        memRefSyntax)
                    : new MemRefType(
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeShape(memRefSyntax.Dimensions),
                        BindTypeReference(memRefSyntax.ElementType),
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 0)!,
                        MLIR.Dialects.Builtin.BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 1)!,
                        memRefSyntax);
            default:
                Report(new AssemblyDiagnostic(syntax.Location, $"Unsupported type syntax '{syntax.GetType().Name}'."));
                return new UnknownTypeReference(syntax, null, null);
        }
    }

    private TypeReference? TryBindStructuredTypeDefinition(TypeSyntax syntax)
    {
        var canonicalName = GetStructuredTypeDefinitionName(syntax);
        if (canonicalName == null || dialectRegistry == null || !dialectRegistry.TryGetType(canonicalName, out var definition))
        {
            return null;
        }

        // If an assembly format is registered, let it drive binding. Otherwise fall back to an
        // UnknownTypeReference that carries the definition metadata so callers can still identify
        // the type family.
        return definition.AssemblyFormat != null
            ? definition.AssemblyFormat.Bind(syntax, this)
            : new UnknownTypeReference(syntax, canonicalName, definition);
    }

    /// <summary>
    /// Binds a type reference syntax tree to a semantic type reference.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic type reference.</returns>
    public TypeReference BindTypeReference(RawSyntaxText syntax)
    {
        return BindTypeReference(ReparseTypeSyntax(syntax));
    }

    internal AttributeValueSyntax ReparseAttributeValueSyntax(RawSyntaxText rawSyntax, AttributeConstraintDefinition? expectedDefinition = null)
    {
        return Parser.ParseAttributeValue(rawSyntax.Text, dialectRegistry, expectedDefinition);
    }

    internal TypeSyntax ReparseTypeSyntax(RawSyntaxText rawSyntax)
    {
        return Parser.ParseType(rawSyntax.Text, dialectRegistry);
    }

    private TypeReference BindTypeReferenceCore(TypeSyntax syntaxNode, RawSyntaxText rawSyntax)
    {
        var canonicalName = TryGetTypeDefinitionName(rawSyntax.Text);
        TypeDefinition? definition = null;
        if (canonicalName != null && dialectRegistry != null)
        {
            dialectRegistry.TryGetType(canonicalName, out definition);
        }

        TypeReference type;
        if (definition != null)
        {
            // If an assembly format is registered, let it drive binding. Otherwise fall back to an
            // UnknownTypeReference that carries the definition metadata so callers can still identify
            // the type family.
            type = definition.AssemblyFormat != null
                ? definition.AssemblyFormat.Bind(syntaxNode, this)
                : new UnknownTypeReference(syntaxNode, canonicalName, definition);
        }
        else
        {
            type = new UnknownTypeReference(syntaxNode, canonicalName, definition);
        }

        return type;
    }

    private static IReadOnlyList<TypeSyntax> GetFunctionResults(FunctionTypeSyntax syntax)
    {
        return syntax.HasDelimitedResults
            ? syntax.ResultTypes.Items
            : syntax.ResultType != null ? [syntax.ResultType] : [];
    }

    private static string? GetStructuredTypeDefinitionName(TypeSyntax syntax)
    {
        switch (syntax)
        {
            case BuiltinIntegerTypeSyntax:
                return "builtin.integer";
            case BuiltinFloatTypeSyntax floatSyntax:
                return "builtin." + floatSyntax.Name;
            case BuiltinIndexTypeSyntax:
                return "builtin.index";
            case BuiltinNoneTypeSyntax:
                return "builtin.none";
            case TupleTypeSyntax:
                return "builtin.tuple";
            case FunctionTypeSyntax:
                return "builtin.function";
            case TensorTypeSyntax tensorSyntax:
                return tensorSyntax.IsUnranked ? "builtin.unranked_tensor" : "builtin.tensor";
            case VectorTypeSyntax:
                return "builtin.vector";
            case MemRefTypeSyntax memRefSyntax:
                return memRefSyntax.IsUnranked ? "builtin.unranked_memref" : "builtin.memref";
            case DialectNamedTypeSyntax namedTypeSyntax:
                return namedTypeSyntax.Name;
            default:
                return null;
        }
    }

    private static string? TryGetAttributeDefinitionName(string text)
    {
        if (string.IsNullOrEmpty(text) || text[0] != '#')
        {
            return null;
        }

        return ReadQualifiedName(text, 1);
    }

    private static string? TryGetTypeDefinitionName(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text[0] == '!')
        {
            return ReadQualifiedName(text, 1);
        }

        if (char.IsLetter(text[0]) || text[0] == '_')
        {
            return ReadBareName(text);
        }

        return null;
    }

    private static string? ReadQualifiedName(string text, int startIndex)
    {
        var index = startIndex;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_' || text[index] == '.'))
        {
            index++;
        }

        return index > startIndex ? text.Substring(startIndex, index - startIndex) : null;
    }

    private static string? ReadBareName(string text)
    {
        var index = 0;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_' || text[index] == '.'))
        {
            index++;
        }

        return index > 0 ? text.Substring(0, index) : null;
    }

    private void PushValueScope()
    {
        valueScopes.Push(new Dictionary<string, Value>());
    }

    private void PopValueScope()
    {
        valueScopes.Pop();
    }

    private void DefineValue(Value value)
    {
        valueScopes.Peek()[value.Name] = value;
    }

    private bool TryLookupValue(string name, out Value value)
    {
        foreach (var scope in valueScopes)
        {
            if (scope.TryGetValue(name, out value!))
            {
                return true;
            }
        }

        value = null!;
        return false;
    }
}
