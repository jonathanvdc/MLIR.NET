namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;
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
            foreach (var result in boundOperation.ResultValues)
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
    /// the corresponding definition will be used to construct a typed operation; otherwise, an <see cref="UnknownOperation"/> will be created.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic operation.</returns>
    public Operation BindOperation(OperationSyntax syntax)
    {
        var name = NormalizeOperationName(syntax.Name);
        OperationDefinition? definition = null;
        if (dialectRegistry != null)
        {
            dialectRegistry.TryGetOperation(name, out definition);
        }

        if (syntax.Body is GenericOperationBodySyntax genericBody)
        {
            return BindGenericOperation(syntax, genericBody, name, definition);
        }
        else if (definition != null && definition.AssemblyFormat != null)
        {
            return definition.AssemblyFormat.Bind(syntax, definition, this);
        }
        else
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Unrecognized operation '{name}' with no generic body and no assembly format defined."));
            return new UninterpretedOperation(syntax, name);
        }
    }

    internal Operation BindGenericOperation(OperationSyntax syntax, GenericOperationBodySyntax body, string name, OperationDefinition? definition)
    {
        var regions = new List<Region>();
        foreach (var region in body.Regions)
        {
            regions.Add(BindRegion(region));
        }

        var attributeList = new List<NamedAttribute>();
        foreach (var attribute in body.Attributes)
        {
            attributeList.Add(BindNamedAttribute(attribute));
        }

        var attributes = new NamedAttributeCollection(attributeList);

        TypeReference? typeSignatureReference = null;
        if (body.TypeSignatureSyntax != null)
        {
            typeSignatureReference = BindTypeReference(body.TypeSignatureSyntax);
        }

        var resultValues = BindOperationResults(syntax.ResultTokens);
        var operandValues = BindValueUses(body.OperandList.Items);
        var successorReferences = BindBlockReferences(body.SuccessorList.Items);
        Operation operation;
        if (definition != null && CheckGenericOperationConstraints(syntax, definition, regions, attributes, typeSignatureReference, resultValues, operandValues, successorReferences))
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
                successorReferences);
            operation = definition.Factory(constructionContext);
        }
        else
        {
            operation = new UnknownOperation(
                syntax,
                name,
                definition,
                regions,
                attributes,
                typeSignatureReference,
                resultValues,
                operandValues,
                successorReferences);
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
        IReadOnlyList<BlockReference> successorReferences)
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

        if (definition.SuccessorCount.HasValue && successorReferences.Count != definition.SuccessorCount.Value)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected exactly {definition.SuccessorCount.Value} successor(s) but found {successorReferences.Count}."));
            isValid = false;
        }
        else if (successorReferences.Count < definition.SuccessorMinCount)
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Expected at least {definition.SuccessorMinCount} successor(s) but found {successorReferences.Count}."));
            isValid = false;
        }

        foreach (var requiredAttribute in definition.RequiredAttributes)
        {
            if (!attributes.Any(a => a.Name == requiredAttribute))
            {
                Report(new AssemblyDiagnostic(syntax.Location, $"{definition.Name} expects a '{requiredAttribute}' required attribute."));
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
        var blocks = new List<Block>();
        foreach (var block in syntax.Blocks)
        {
            blocks.Add(BindBlock(block));
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
            foreach (var result in boundOperation.ResultValues)
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
    public Value BindValueReference(SyntaxToken token)
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
    public IReadOnlyList<Value> BindValueUses(IReadOnlyList<SyntaxToken> tokens)
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
    public IReadOnlyList<OperationResult> BindOperationResults(IReadOnlyList<SyntaxToken> tokens)
    {
        var values = new List<OperationResult>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(new OperationResult(token));
        }

        return values;
    }

    /// <summary>
    /// Binds a syntax token to a block reference, which may refer to a block defined by another operation in the same module.
    /// The token text is expected to be in the form of a block name (e.g. "^bb1", "^foo", etc.); otherwise, the resulting reference may be invalid.
    /// </summary>
    /// <param name="token">The syntax token to bind.</param>
    /// <returns>The semantic block reference.</returns>
    public BlockReference BindBlockReference(SyntaxToken token)
    {
        return new BlockReference(token);
    }

    /// <summary>
    /// Creates block references for the given tokens, which may refer to blocks defined by other operations in the same module.
    /// </summary>
    /// <param name="tokens">The tokens for which to create references.</param>
    /// <returns>The list of block references.</returns>
    public IReadOnlyList<BlockReference> BindBlockReferences(IReadOnlyList<SyntaxToken> tokens)
    {
        var values = new List<BlockReference>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(BindBlockReference(token));
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
        return BindAttributeValue(syntax, (AttributeDefinition?)null);
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value, preferring the supplied expected attribute definition when known.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="expectedDefinitionName">The expected parser-facing attribute definition name, if one is known.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(AttributeValueSyntax syntax, string? expectedDefinitionName)
    {
        AttributeDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeForParsing(expectedDefinitionName!, out expectedDefinition);
        }

        return BindAttributeValue(syntax, expectedDefinition);
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value, preferring the supplied expected attribute definition when known.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="expectedDefinition">The expected attribute definition, if one is known.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(AttributeValueSyntax syntax, AttributeDefinition? expectedDefinition)
    {
        if (syntax.TryGetRawText(out var rawAttributeValueSyntax))
        {
            return BindAttributeValueCore(syntax, rawAttributeValueSyntax!, expectedDefinition);
        }
        else
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Unsupported attribute value syntax '{syntax.GetType().Name}'."));
            return new UnknownAttributeValue(syntax, null, null, syntax.Location);
        }
    }

    /// <summary>
    /// Binds an attribute value syntax tree to a semantic attribute value.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic attribute value.</returns>
    public AttributeValue BindAttributeValue(RawSyntaxText syntax)
    {
        return BindAttributeValueCore(new RawAttributeValueSyntax(syntax), syntax, null);
    }

    private AttributeValue BindAttributeValueCore(AttributeValueSyntax syntaxNode, RawSyntaxText rawSyntax, AttributeDefinition? expectedDefinition)
    {
        var canonicalName = TryGetAttributeDefinitionName(rawSyntax.Text);
        AttributeDefinition? definition = null;
        if (expectedDefinition != null)
        {
            definition = expectedDefinition;
        }

        if (definition == null && canonicalName != null && dialectRegistry != null)
        {
            dialectRegistry.TryGetAttribute(canonicalName, out definition);
        }

        AttributeValue attribute;
        var location = syntaxNode.Location;
        if (definition != null)
        {
            attribute = definition.AssemblyFormat != null
                ? definition.AssemblyFormat.Bind(syntaxNode, definition, this)
                : definition.Factory(new AttributeValueConstructionContext(syntaxNode, canonicalName, definition, location));
        }
        else
        {
            attribute = new UnknownAttributeValue(syntaxNode, canonicalName, definition, location);
        }

        return attribute;
    }

    /// <summary>
    /// Binds a named attribute syntax tree to a semantic named attribute, which includes both the attribute name and value.
    /// </summary>
    /// <param name="syntax">The named attribute syntax tree to bind.</param>
    /// <returns>The semantic named attribute.</returns>
    public NamedAttribute BindNamedAttribute(NamedAttributeSyntax syntax)
    {
        var value = BindAttributeValue(syntax.ValueSyntax);
        return new NamedAttribute(syntax, value);
    }

    /// <summary>
    /// Binds a type reference syntax tree to a semantic type reference.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic type reference.</returns>
    public TypeReference BindTypeReference(TypeSyntax syntax)
    {
        if (syntax.TryGetRawText(out var rawTypeSyntax))
        {
            return BindTypeReferenceCore(syntax, rawTypeSyntax!);
        }
        else
        {
            Report(new AssemblyDiagnostic(syntax.Location, $"Unsupported type syntax '{syntax.GetType().Name}'."));
            return new UnknownTypeReference(syntax, null, null, syntax.Location);
        }
    }

    /// <summary>
    /// Binds a type reference syntax tree to a semantic type reference.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <returns>The semantic type reference.</returns>
    public TypeReference BindTypeReference(RawSyntaxText syntax)
    {
        // TODO: make this internal in the future and only expose the TypeSyntax overload publicly once we have more robust support for different type syntax forms.
        return BindTypeReferenceCore(new RawTypeSyntax(syntax), syntax);
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
            type = definition.AssemblyFormat != null
                ? definition.AssemblyFormat.Bind(syntaxNode, definition, this)
                : definition.Factory(new TypeReferenceConstructionContext(syntaxNode, canonicalName, definition, syntaxNode.Location));
        }
        else
        {
            type = new UnknownTypeReference(syntaxNode, canonicalName, definition, syntaxNode.Location);
        }

        return type;
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
