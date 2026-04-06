namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Shared document-level state for TableGen evaluation.
/// </summary>
internal sealed class EvaluationContext
{
    /// <summary>
    /// Indexes the parsed document into lookup tables used throughout evaluation.
    /// </summary>
    /// <param name="document">The parsed document being interpreted.</param>
    public EvaluationContext(DocumentSyntax document)
    {
        Document = document;
        Classes = document.Declarations
            .OfType<ClassSyntax>()
            .ToDictionary(static c => c.Name, static c => c);
        Definitions = document.Declarations.OfType<DefSyntax>().ToList();
        DefinitionsByName = Definitions.ToDictionary(static definition => definition.Name, static definition => definition);
        ExtendsDeclarations = document.Declarations.OfType<ExtendsSyntax>().ToList();
    }

    /// <summary>
    /// Gets the original parsed document being evaluated.
    /// </summary>
    public DocumentSyntax Document { get; }

    /// <summary>
    /// Gets all class declarations keyed by class name.
    /// </summary>
    public IReadOnlyDictionary<string, ClassSyntax> Classes { get; }

    /// <summary>
    /// Gets the top-level record definitions in source order.
    /// </summary>
    public IReadOnlyList<DefSyntax> Definitions { get; }

    /// <summary>
    /// Gets the top-level record definitions keyed by record name.
    /// </summary>
    public IReadOnlyDictionary<string, DefSyntax> DefinitionsByName { get; }

    /// <summary>
    /// Gets the top-level overlay declarations in source order.
    /// </summary>
    public IReadOnlyList<ExtendsSyntax> ExtendsDeclarations { get; }

    /// <summary>
    /// Gets the already-evaluated values of top-level <c>defvar</c> declarations.
    /// </summary>
    public Dictionary<string, Value> DefvarValues { get; } = new();

    /// <summary>
    /// Caches <c>ClassIsA</c> answers so repeated <c>!isa</c> checks do not repeatedly walk inheritance chains.
    /// </summary>
    public Dictionary<(string ClassName, string TypeName), bool> ClassIsACache { get; } = new();
}
