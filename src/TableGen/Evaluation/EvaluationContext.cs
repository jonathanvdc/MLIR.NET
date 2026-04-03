namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Shared document-level state for TableGen evaluation.
/// </summary>
internal sealed class EvaluationContext
{
    public EvaluationContext(DocumentSyntax document)
    {
        Document = document;
        Classes = document.Declarations
            .OfType<ClassSyntax>()
            .ToDictionary(static c => c.Name, static c => c);
        Definitions = document.Declarations.OfType<DefSyntax>().ToList();
        DefinitionsByName = Definitions.ToDictionary(static definition => definition.Name, static definition => definition);
    }

    public DocumentSyntax Document { get; }

    public IReadOnlyDictionary<string, ClassSyntax> Classes { get; }

    public IReadOnlyList<DefSyntax> Definitions { get; }

    public IReadOnlyDictionary<string, DefSyntax> DefinitionsByName { get; }

    public Dictionary<string, Value> DefvarValues { get; } = new();

    public Dictionary<(string ClassName, string TypeName), bool> ClassIsACache { get; } = new();
}
