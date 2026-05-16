namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Lowers the parsed declarative assembly-format elements into a flat list of slots.
/// Unsupported constructs are recorded as plan diagnostics so generated code can fail loudly.
/// </summary>
internal sealed class AssemblyFormatPlanCompiler
{
    private readonly FormatSubject subject;
    private readonly List<FormatSlot> slots = [];
    private readonly List<string> unsupported = [];
    private int ordinal;

    public AssemblyFormatPlanCompiler(FormatSubject subject)
    {
        this.subject = subject;
    }

    public AssemblyFormatPlan Compile()
    {
        foreach (var element in subject.Elements)
        {
            Lower(element);
        }

        return new AssemblyFormatPlan(subject, slots, unsupported.Distinct(StringComparer.Ordinal).ToArray());
    }

    private void Lower(Element element)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var literalElement in literal.Value)
                {
                    LowerLiteral(literalElement);
                }
                break;
            case VariableChunk variable:
                AddResolved(subject.ResolveVariable(variable, ordinal), "variable $" + variable.Name);
                ordinal++;
                break;
            case OilistDirectiveChunk:
                unsupported.Add("oilist");
                break;
            case DirectiveChunk directive:
                AddResolved(subject.ResolveDirective(directive, ordinal), GetFeatureName(directive));
                ordinal++;
                break;
            case OptionalGroup:
                unsupported.Add("optional group");
                break;
            default:
                unsupported.Add(element.GetType().Name);
                break;
        }
    }

    private void LowerLiteral(Literal literal)
    {
        switch (literal)
        {
            case PunctuationLiteral punctuation:
                slots.Add(FormatSlot.ForLiteral(
                    "literal" + ordinal.ToString(CultureInfo.InvariantCulture),
                    EmitterHelpers.GetPunctuationText(punctuation.TokenKind),
                    "global::MLIR.Text.TokenKind." + punctuation.TokenKind));
                ordinal++;
                break;
            case KeywordLiteral keyword:
                slots.Add(FormatSlot.ForLiteral(
                    "literal" + ordinal.ToString(CultureInfo.InvariantCulture),
                    keyword.Spelling,
                    "global::MLIR.Text.TokenKind.Identifier",
                    isKeyword: true));
                ordinal++;
                break;
            case WhitespaceLiteral:
            case NewlineLiteral:
            case EmptyLiteral:
                break;
            default:
                unsupported.Add(literal.GetType().Name);
                break;
        }
    }

    private void AddResolved(FormatSlot? slot, string featureName)
    {
        if (slot == null)
        {
            unsupported.Add(featureName);
            return;
        }

        slots.Add(slot);
    }

    private static string GetFeatureName(DirectiveChunk directive)
    {
        return directive switch
        {
            AttrDictDirectiveChunk => "attr-dict",
            TypeDirectiveChunk => "type directive",
            _ => directive.GetType().Name,
        };
    }
}
