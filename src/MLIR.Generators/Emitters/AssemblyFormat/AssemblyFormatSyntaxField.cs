namespace MLIR.Generators.Emitters.AssemblyFormat;

using MLIR.ODS.Model;
using MLIR.Text;

internal enum AssemblyFormatSyntaxFieldKind
{
    Token,
    SyntaxNode,
    Trivia,
    DelimitedList,
    RegionList,
    TypeList,
    Unknown,
}

internal abstract class AssemblyFormatSyntaxField
{
    protected AssemblyFormatSyntaxField(
        AssemblyFormatSyntaxFieldKind kind,
        string name,
        string csType,
        string writeToCode)
    {
        Kind = kind;
        Name = name;
        CsType = csType;
        WriteToCode = writeToCode;
    }

    public AssemblyFormatSyntaxFieldKind Kind { get; }

    public string Name { get; }

    public string CsType { get; }

    public string WriteToCode { get; }
}

internal sealed class LiteralTokenField : AssemblyFormatSyntaxField
{
    public LiteralTokenField(string localName, string syntheticText, string kindExpr, bool isKeyword)
        : base(AssemblyFormatSyntaxFieldKind.Token, localName, "Token", string.Empty)
    {
        SyntheticText = syntheticText;
        KindExpr = kindExpr;
        IsKeyword = isKeyword;
    }

    public string LocalName => Name;

    public string SyntheticText { get; }

    public string KindExpr { get; }

    public bool IsKeyword { get; }
}

internal sealed class VariableSyntaxField : AssemblyFormatSyntaxField
{
    public VariableSyntaxField(
        string name,
        string syntaxType,
        SyntaxValueShape syntaxShape,
        AttrOrTypeParameterModel? paramModel)
        : base(AssemblyFormatSyntaxFieldKind.SyntaxNode, name, syntaxType, string.Empty)
    {
        SyntaxShape = syntaxShape;
        ParamModel = paramModel;
    }

    public string SyntaxType => CsType;

    public SyntaxValueShape SyntaxShape { get; }

    public AttrOrTypeParameterModel? ParamModel { get; }
}

internal sealed class TriviaSyntaxField : AssemblyFormatSyntaxField
{
    public TriviaSyntaxField(string text, bool isNewline)
        : base(AssemblyFormatSyntaxFieldKind.Trivia, string.Empty, "string", string.Empty)
    {
        Text = text;
        IsNewline = isNewline;
    }

    public string Text { get; }

    public bool IsNewline { get; }
}
