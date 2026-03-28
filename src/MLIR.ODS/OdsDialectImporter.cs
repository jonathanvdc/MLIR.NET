namespace MLIR.ODS;

using System.Collections.Generic;
using MLIR.ODS.Model;
using TableGen.Evaluation;

/// <summary>
/// Translates interpreted TableGen records into a coarse ODS model.
/// </summary>
public static class OdsDialectImporter
{
    /// <summary>
    /// Imports dialect models from an interpreted TableGen document.
    /// </summary>
    /// <remarks>
    /// This is currently just a scaffolded entry point. Real ODS interpretation will populate
    /// dialects, operations, attributes, and types from MLIR-specific TableGen conventions.
    /// </remarks>
    public static IReadOnlyList<OdsDialectModel> Import(InterpretedDocument document)
    {
        return EmptyDialects;
    }

    private static readonly IReadOnlyList<OdsDialectModel> EmptyDialects = new OdsDialectModel[0];
}
