namespace MLIR.Generators.Emitters.AssemblyFormat;

using MLIR.ODS.Model;
using MLIR.Text;

internal abstract class FormatSlot
{
}

internal sealed class LiteralTokenSlot : FormatSlot
{
    public string LocalName { get; set; } = string.Empty;

    public string SyntheticText { get; set; } = string.Empty;

    public string KindExpr { get; set; } = string.Empty;

    public bool IsKeyword { get; set; }
}

internal sealed class VariableSlot : FormatSlot
{
    public string Name { get; set; } = string.Empty;

    public string SyntaxType { get; set; } = "AttributeValueSyntax";

    public SyntaxValueShape SyntaxShape { get; set; } = SyntaxValueShape.SyntaxNode;

    public AttrOrTypeParameterModel? ParamModel { get; set; }
}

internal sealed class TriviaSlot : FormatSlot
{
    public string Text { get; set; } = string.Empty;

    public bool IsNewline { get; set; }
}
