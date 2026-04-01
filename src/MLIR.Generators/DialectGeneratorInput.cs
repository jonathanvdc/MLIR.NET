namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MLIR.ODS;
using MLIR.ODS.Model;
using TableGen;

internal static class DialectGeneratorInput
{
    public static ParsedDialectFile ParseFile(
        AdditionalText file,
        TableGenIncludeResolver? resolver,
        System.Threading.CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken);
        if (text == null)
        {
            return new ParsedDialectFile(file.Path, EmptyDialects, "Could not read the additional text.");
        }

        try
        {
            var sourceFile = new TableGenSourceFile(file.Path);
            var document = resolver != null
                ? Document.Load(text.ToString(), resolver, sourceFile)
                : Document.Parse(text.ToString());
            var dialects = DialectImporter.Import(document.Evaluate());
            return new ParsedDialectFile(file.Path, dialects, null);
        }
        catch (Exception exception)
        {
            return new ParsedDialectFile(file.Path, EmptyDialects, exception.Message);
        }
    }

    private static readonly IReadOnlyList<DialectModel> EmptyDialects = new DialectModel[0];
}
