namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MLIR.ODS;
using MLIR.ODS.Model;
using TableGen;

internal static class OdsDialectGeneratorInput
{
    public static ParsedDialectFile ParseFile(AdditionalText file, System.Threading.CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken);
        if (text == null)
        {
            return new ParsedDialectFile(file.Path, EmptyDialects, "Could not read the additional text.");
        }

        try
        {
            var document = TableGenDocument.Parse(text.ToString());
            var dialects = OdsDialectImporter.Import(document.Evaluate());
            return new ParsedDialectFile(file.Path, dialects, null);
        }
        catch (Exception exception)
        {
            return new ParsedDialectFile(file.Path, EmptyDialects, exception.Message);
        }
    }

    private static readonly IReadOnlyList<OdsDialectModel> EmptyDialects = new OdsDialectModel[0];
}
