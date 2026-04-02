namespace MLIR.Dialects;

using System;
using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Provides a fluent API for constructing <see cref="OperationDefinition"/> instances.
/// </summary>
public sealed class OperationDefinitionBuilder
{
    private readonly string name;
    private readonly List<OperationSegmentDefinition> operandDefinitions = new List<OperationSegmentDefinition>();
    private readonly List<OperationSegmentDefinition> resultDefinitions = new List<OperationSegmentDefinition>();
    private readonly List<OperationSegmentDefinition> regionDefinitions = new List<OperationSegmentDefinition>();
    private readonly List<OperationSegmentDefinition> successorDefinitions = new List<OperationSegmentDefinition>();
    private readonly List<OperationAttributeDefinition> attributeDefinitions = new List<OperationAttributeDefinition>();
    private IOperationVerifier? verifier;
    private IOperationAssemblyFormat? assemblyFormat;
    private Func<OperationConstructionContext, Operation> factory = static context => new GenericOperation(
        context.Syntax,
        context.Name,
        context.Definition,
        context.Regions,
        context.Attributes,
        context.TypeSignatureReference,
        context.ResultValues,
        context.OperandValues,
        context.Successors);

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationDefinitionBuilder"/> class.
    /// </summary>
    /// <param name="name">The canonical operation name without MLIR string-literal quoting.</param>
    public OperationDefinitionBuilder(string name)
    {
        this.name = name;
    }

    /// <summary>
    /// Adds a fixed operand segment.
    /// </summary>
    public OperationDefinitionBuilder Operand(string name)
    {
        operandDefinitions.Add(new OperationSegmentDefinition(name));
        return this;
    }

    /// <summary>
    /// Adds a variadic operand segment.
    /// </summary>
    public OperationDefinitionBuilder VariadicOperand(string name)
    {
        operandDefinitions.Add(new OperationSegmentDefinition(name, isVariadic: true));
        return this;
    }

    /// <summary>
    /// Adds a fixed result segment.
    /// </summary>
    public OperationDefinitionBuilder Result(string name)
    {
        resultDefinitions.Add(new OperationSegmentDefinition(name));
        return this;
    }

    /// <summary>
    /// Adds a variadic result segment.
    /// </summary>
    public OperationDefinitionBuilder VariadicResult(string name)
    {
        resultDefinitions.Add(new OperationSegmentDefinition(name, isVariadic: true));
        return this;
    }

    /// <summary>
    /// Adds a fixed region segment.
    /// </summary>
    public OperationDefinitionBuilder Region(string name)
    {
        regionDefinitions.Add(new OperationSegmentDefinition(name));
        return this;
    }

    /// <summary>
    /// Adds a variadic region segment.
    /// </summary>
    public OperationDefinitionBuilder VariadicRegion(string name)
    {
        regionDefinitions.Add(new OperationSegmentDefinition(name, isVariadic: true));
        return this;
    }

    /// <summary>
    /// Adds a fixed successor segment.
    /// </summary>
    public OperationDefinitionBuilder Successor(string name)
    {
        successorDefinitions.Add(new OperationSegmentDefinition(name));
        return this;
    }

    /// <summary>
    /// Adds a variadic successor segment.
    /// </summary>
    public OperationDefinitionBuilder VariadicSuccessor(string name)
    {
        successorDefinitions.Add(new OperationSegmentDefinition(name, isVariadic: true));
        return this;
    }

    /// <summary>
    /// Adds a required attribute definition.
    /// </summary>
    public OperationDefinitionBuilder RequiredAttribute(string name, AttributeConstraintDefinition? constraintDefinition = null)
    {
        attributeDefinitions.Add(new OperationAttributeDefinition(name, constraintDefinition: constraintDefinition));
        return this;
    }

    /// <summary>
    /// Adds an optional attribute definition.
    /// </summary>
    public OperationDefinitionBuilder OptionalAttribute(string name, AttributeConstraintDefinition? constraintDefinition = null)
    {
        attributeDefinitions.Add(new OperationAttributeDefinition(name, isRequired: false, constraintDefinition: constraintDefinition));
        return this;
    }

    /// <summary>
    /// Sets an exact operand count constraint.
    /// </summary>
    public OperationDefinitionBuilder WithOperandCount(int count)
    {
        operandDefinitions.Clear();
        for (var i = 0; i < count; i++)
        {
            operandDefinitions.Add(new OperationSegmentDefinition("operand" + i));
        }

        return this;
    }

    /// <summary>
    /// Sets an exact result count constraint.
    /// </summary>
    public OperationDefinitionBuilder WithResultCount(int count)
    {
        resultDefinitions.Clear();
        for (var i = 0; i < count; i++)
        {
            resultDefinitions.Add(new OperationSegmentDefinition("result" + i));
        }

        return this;
    }

    /// <summary>
    /// Sets an exact region count constraint.
    /// </summary>
    public OperationDefinitionBuilder WithRegionCount(int count)
    {
        regionDefinitions.Clear();
        for (var i = 0; i < count; i++)
        {
            regionDefinitions.Add(new OperationSegmentDefinition("region" + i));
        }

        return this;
    }

    /// <summary>
    /// Sets an exact successor count constraint.
    /// </summary>
    public OperationDefinitionBuilder WithSuccessorCount(int count)
    {
        successorDefinitions.Clear();
        for (var i = 0; i < count; i++)
        {
            successorDefinitions.Add(new OperationSegmentDefinition("successor" + i));
        }

        return this;
    }

    /// <summary>
    /// Registers a custom verifier.
    /// </summary>
    public OperationDefinitionBuilder WithVerifier(IOperationVerifier operationVerifier)
    {
        verifier = operationVerifier;
        return this;
    }

    /// <summary>
    /// Registers a custom verifier delegate.
    /// </summary>
    public OperationDefinitionBuilder WithVerifier(Action<Operation, VerificationContext> action)
    {
        verifier = new DelegateOperationVerifier(action);
        return this;
    }

    /// <summary>
    /// Registers a custom assembly format.
    /// </summary>
    public OperationDefinitionBuilder WithAssemblyFormat(IOperationAssemblyFormat format)
    {
        assemblyFormat = format;
        return this;
    }

    /// <summary>
    /// Registers a typed operation factory.
    /// </summary>
    public OperationDefinitionBuilder WithFactory(Func<OperationConstructionContext, Operation> operationFactory)
    {
        factory = operationFactory;
        return this;
    }

    /// <summary>
    /// Builds the operation definition.
    /// </summary>
    public OperationDefinition Build()
    {
        return new OperationDefinition(
            name,
            operandDefinitions,
            resultDefinitions,
            regionDefinitions,
            successorDefinitions,
            attributeDefinitions,
            verifier,
            assemblyFormat,
            factory);
    }
}
