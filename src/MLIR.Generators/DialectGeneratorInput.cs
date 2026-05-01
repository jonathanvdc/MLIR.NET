namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MLIR.ODS;
using MLIR.ODS.Model;
using MLIR.Text;
using TableGen;

internal static class DialectGeneratorInput
{
    public static ParsedDialectFile ParseFile(
        AdditionalText file,
        IncludeResolver? resolver,
        System.Threading.CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken);
        if (text == null)
        {
            return new ParsedDialectFile(file.Path, EmptyDialects, "Could not read the additional text.");
        }

        return ParseSource(file.Path, text.ToString(), resolver);
    }

    public static ParsedDialectFile ParseFile(
        TableGenInput input,
        IncludeResolver? resolver)
    {
        return ParseSource(input.Path, input.SourceText, resolver);
    }

    private static ParsedDialectFile ParseSource(
        string path,
        string sourceText,
        IncludeResolver? resolver)
    {
        try
        {
            var sourceDocument = new StringDocument(path, sourceText);
            var documentResult = resolver != null
                ? Document.Load(sourceDocument, resolver)
                : Document.Parse(sourceDocument);
            if (!documentResult.IsSuccess)
            {
                var diagnostic = documentResult.Diagnostic!;
                return new ParsedDialectFile(path, EmptyDialects, diagnostic.Message, diagnostic);
            }

            var evaluated = documentResult.Value.Evaluate();
            if (!evaluated.IsSuccess)
            {
                var diagnostic = evaluated.Diagnostic!;
                return new ParsedDialectFile(path, EmptyDialects, diagnostic.Message, diagnostic);
            }

            var dialects = DialectImporter.Import(evaluated.Value);
            return new ParsedDialectFile(path, dialects, null);
        }
        catch (Exception exception)
        {
            return new ParsedDialectFile(path, EmptyDialects, exception.Message);
        }
    }

    private static readonly IReadOnlyList<DialectModel> EmptyDialects = new DialectModel[0];
}
