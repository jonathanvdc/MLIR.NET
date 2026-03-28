namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Text;

/// <summary>
/// Binds generic MLIR concrete syntax to semantic nodes using a dialect registry.
/// </summary>
public static class MlirBinder
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
        var regions = new List<Region>();
        foreach (var region in syntax.Regions)
        {
            regions.Add(BindRegion(region, dialectRegistry, diagnostics));
        }

        var attributes = new List<NamedAttribute>();
        foreach (var attribute in syntax.Attributes)
        {
            attributes.Add(new NamedAttribute(attribute));
        }

        var name = NormalizeOperationName(syntax.Name);
        OperationDefinition? definition = null;
        if (dialectRegistry != null)
        {
            dialectRegistry.TryGetOperation(name, out definition);
        }

        var properties = new Dictionary<string, object?>();
        var operation = new Operation(syntax, name, definition, regions, attributes, properties);
        if (definition?.AssemblyFormat != null)
        {
            definition.AssemblyFormat.Bind(operation, new OperationAssemblyBindingContext(operation, properties, diagnostics));
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
            arguments.Add(new BlockArgument(argument));
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
}
