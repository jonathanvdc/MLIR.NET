namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Lowers the parsed declarative assembly-format elements into the generated format tree.
/// Unsupported constructs are recorded as plan diagnostics so generated code can fail loudly.
/// </summary>
internal sealed class AssemblyFormatPlanCompiler
{
    private readonly FormatSubject subject;
    private readonly List<FormatNode> nodes = [];
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
            Lower(element, nodes);
        }

        return new AssemblyFormatPlan(subject, nodes, unsupported.Distinct(StringComparer.Ordinal).ToArray());
    }

    private void Lower(Element element, List<FormatNode> target)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var literalElement in literal.Value)
                {
                    LowerLiteral(literalElement, target);
                }
                break;
            case VariableChunk variable:
                AddResolved(target, subject.ResolveVariable(variable, ordinal), "variable $" + variable.Name);
                ordinal++;
                break;
            case OilistDirectiveChunk oilist:
                LowerOilist(oilist, target);
                break;
            case DirectiveChunk directive:
                AddResolved(target, subject.ResolveDirective(directive, ordinal), GetFeatureName(directive));
                ordinal++;
                break;
            case OptionalGroup optionalGroup:
                LowerOptionalGroup(optionalGroup, target);
                break;
            default:
                unsupported.Add(element.GetType().Name);
                break;
        }
    }

    private void LowerOptionalGroup(OptionalGroup optionalGroup, List<FormatNode> target)
    {
        if (optionalGroup.ElseElements is { Count: > 0 })
        {
            unsupported.Add("optional group else branch");
            return;
        }

        var groupNodes = new List<FormatNode>();
        foreach (var element in optionalGroup.ThenElements)
        {
            Lower(element, groupNodes);
        }

        var name = DialectGeneratorNaming.ToPascalCase(optionalGroup.AnchorName) + "Group";
        target.Add(new OptionalGroupNode(
            name,
            subject.SyntaxClassName + name + "Syntax",
            optionalGroup.AnchorName,
            groupNodes));
    }

    private void LowerOilist(OilistDirectiveChunk oilist, List<FormatNode> target)
    {
        var clauses = new List<OptionalGroupNode>();
        foreach (var clause in oilist.Clauses)
        {
            var clauseNodes = new List<FormatNode>
            {
                FormatSlot.ForLiteral(
                    clause.Keyword + "Keyword",
                    clause.Keyword,
                    "global::MLIR.Text.TokenKind.Identifier",
                    isKeyword: true),
            };

            ordinal++;
            foreach (var element in clause.Elements)
            {
                LowerOilistElement(element, clauseNodes);
            }

            var anchor = clauseNodes
                .OfType<FormatSlot>()
                .FirstOrDefault(static slot => slot is AttributeValueSlot or TypeSlot or SsaValueSlot or SsaValueListSlot)
                ?.SourceName ?? clause.Keyword + "Keyword";
            var name = DialectGeneratorNaming.ToPascalCase(clause.Keyword) + "Clause";
            clauses.Add(new OptionalGroupNode(
                name,
                subject.SyntaxClassName + name + "Syntax",
                anchor,
                clauseNodes));
        }

        target.Add(new OilistNode(clauses));
    }

    private void LowerOilistElement(OilistElement element, List<FormatNode> target)
    {
        switch (element)
        {
            case OilistLiteralElement literal:
                AddOilistLiteral(literal.Value, target);
                break;
            case OilistVariableElement variable:
                AddResolved(target, subject.ResolveVariable(new VariableChunk(variable.Name), ordinal), "oilist variable $" + variable.Name);
                ordinal++;
                break;
            case OilistTypeDirectiveElement typeDirective:
                AddResolved(target, subject.ResolveDirective(new TypeDirectiveChunk(typeDirective.Operand), ordinal), "oilist type directive");
                ordinal++;
                break;
            default:
                unsupported.Add(element.GetType().Name);
                break;
        }
    }

    private void AddOilistLiteral(string value, List<FormatNode> target)
    {
        target.Add(FormatSlot.ForLiteral(
            "literal" + ordinal.ToString(CultureInfo.InvariantCulture),
            value,
            "global::MLIR.Text.TokenKind.Identifier",
            isKeyword: true));
        ordinal++;
    }

    private void LowerLiteral(Literal literal, List<FormatNode> target)
    {
        switch (literal)
        {
            case PunctuationLiteral punctuation:
                target.Add(FormatSlot.ForLiteral(
                    "literal" + ordinal.ToString(CultureInfo.InvariantCulture),
                    EmitterHelpers.GetPunctuationText(punctuation.TokenKind),
                    "global::MLIR.Text.TokenKind." + punctuation.TokenKind));
                ordinal++;
                break;
            case KeywordLiteral keyword:
                target.Add(FormatSlot.ForLiteral(
                    "literal" + ordinal.ToString(CultureInfo.InvariantCulture),
                    keyword.Spelling,
                    "global::MLIR.Text.TokenKind.Identifier",
                    isKeyword: true));
                ordinal++;
                break;
            case WhitespaceLiteral whitespace:
                target.Add(FormatSlot.ForWhitespace(whitespace.Spaces));
                break;
            case NewlineLiteral:
                target.Add(FormatSlot.ForNewline());
                break;
            case EmptyLiteral:
                break;
            default:
                unsupported.Add(literal.GetType().Name);
                break;
        }
    }

    private void AddResolved(List<FormatNode> target, FormatSlot? slot, string featureName)
    {
        if (slot == null)
        {
            unsupported.Add(featureName);
            return;
        }

        target.Add(slot);
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
