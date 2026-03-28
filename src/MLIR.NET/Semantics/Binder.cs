namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Text;

/// <summary>
/// Binds generic MLIR concrete syntax to semantic nodes using a dialect registry.
/// </summary>
public static class Binder
{
    /// <summary>
    /// Binds a module syntax tree to a semantic module.
    /// </summary>
    /// <param name="syntax">The concrete syntax tree to bind.</param>
    /// <param name="dialectRegistry">The dialect registry used to resolve known operations.</param>
    /// <returns>The semantic module.</returns>
    public static Module BindModule(ModuleSyntax syntax, DialectRegistry? dialectRegistry = null)
    {
        var diagnostics = new List<AssemblyDiagnostic>();
        var operations = new List<Operation>();
        foreach (var operation in syntax.Operations)
        {
            operations.Add(BindOperation(operation, dialectRegistry, diagnostics));
        }

        return new Module(syntax, operations, diagnostics);
    }

    private static Operation BindOperation(OperationSyntax syntax, DialectRegistry? dialectRegistry, List<AssemblyDiagnostic> diagnostics)
    {
        var genericBody = syntax.GenericBody;
        var regions = new List<Region>();
        foreach (var region in genericBody.Regions)
        {
            regions.Add(BindRegion(region, dialectRegistry, diagnostics));
        }

        var attributes = new List<NamedAttribute>();
        foreach (var attribute in genericBody.Attributes)
        {
            attributes.Add(new NamedAttribute(attribute, BindAttributeValue(attribute.RawValue, attribute.NameToken, dialectRegistry, diagnostics)));
        }

        var name = NormalizeOperationName(syntax.Name);
        OperationDefinition? definition = null;
        if (dialectRegistry != null)
        {
            dialectRegistry.TryGetOperation(name, out definition);
        }

        TypeReference? typeSignatureReference = null;
        if (genericBody.RawTypeSignature != null)
        {
            var location = genericBody.TypeSignatureColonToken != null
                ? SourceLocation.FromToken(genericBody.TypeSignatureColonToken.Value)
                : default;
            typeSignatureReference = BindTypeReference(genericBody.RawTypeSignature, location, dialectRegistry, diagnostics);
        }

        var resultValues = CreateValueReferences(syntax.ResultTokens);
        var operandValues = CreateValueReferences(genericBody.OperandList.Items);
        var successorReferences = CreateBlockReferences(genericBody.SuccessorList.Items);
        Operation operation;
        if (definition != null)
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
            definition.AssemblyFormat?.Bind(operation, new OperationAssemblyBindingContext(operation, diagnostics));
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

    private static Region BindRegion(RegionSyntax syntax, DialectRegistry? dialectRegistry, List<AssemblyDiagnostic> diagnostics)
    {
        var blocks = new List<Block>();
        foreach (var block in syntax.Blocks)
        {
            blocks.Add(BindBlock(block, dialectRegistry, diagnostics));
        }

        return new Region(syntax, blocks);
    }

    private static Block BindBlock(BlockSyntax syntax, DialectRegistry? dialectRegistry, List<AssemblyDiagnostic> diagnostics)
    {
        var arguments = new List<BlockArgument>();
        foreach (var argument in syntax.Arguments)
        {
            arguments.Add(new BlockArgument(argument, BindTypeReference(argument.RawType, SourceLocation.FromToken(argument.NameToken), dialectRegistry, diagnostics)));
        }

        var operations = new List<Operation>();
        foreach (var operation in syntax.Operations)
        {
            operations.Add(BindOperation(operation, dialectRegistry, diagnostics));
        }

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

    private static IReadOnlyList<ValueReference> CreateValueReferences(IReadOnlyList<SyntaxToken> tokens)
    {
        var values = new List<ValueReference>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(new ValueReference(token));
        }

        return values;
    }

    private static IReadOnlyList<BlockReference> CreateBlockReferences(IReadOnlyList<SyntaxToken> tokens)
    {
        var values = new List<BlockReference>(tokens.Count);
        foreach (var token in tokens)
        {
            values.Add(new BlockReference(token));
        }

        return values;
    }

    private static AttributeValue BindAttributeValue(RawSyntaxText syntax, SyntaxToken nameToken, DialectRegistry? dialectRegistry, List<AssemblyDiagnostic> diagnostics)
    {
        var canonicalName = TryGetAttributeDefinitionName(syntax.Text);
        AttributeDefinition? definition = null;
        if (canonicalName != null && dialectRegistry != null)
        {
            dialectRegistry.TryGetAttribute(canonicalName, out definition);
        }

        var properties = new Dictionary<string, object?>();
        var attribute = new AttributeValue(syntax, canonicalName, definition, SourceLocation.FromToken(nameToken), properties);
        definition?.AssemblyFormat?.Bind(attribute, new AttributeAssemblyBindingContext(attribute, diagnostics, properties));
        return attribute;
    }

    private static TypeReference BindTypeReference(RawSyntaxText syntax, SourceLocation location, DialectRegistry? dialectRegistry, List<AssemblyDiagnostic> diagnostics)
    {
        var canonicalName = TryGetTypeDefinitionName(syntax.Text);
        TypeDefinition? definition = null;
        if (canonicalName != null && dialectRegistry != null)
        {
            dialectRegistry.TryGetType(canonicalName, out definition);
        }

        var properties = new Dictionary<string, object?>();
        var type = new TypeReference(syntax, canonicalName, definition, location, properties);
        definition?.AssemblyFormat?.Bind(type, new TypeAssemblyBindingContext(type, diagnostics, properties));
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
}
