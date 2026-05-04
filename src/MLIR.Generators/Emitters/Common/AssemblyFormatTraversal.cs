namespace MLIR.Generators.Emitters.Common;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Shared traversal helpers for flattened assembly-format element sequences.
/// </summary>
/// <remarks>
/// These helpers centralize the repeated "walk the format elements and inspect the next
/// literal chunk" logic used by the attribute and operation emitters.
/// </remarks>
internal static class AssemblyFormatTraversal
{
    public static void ForEachElement<T>(IReadOnlyList<T> elements, Action<int, T> onElement)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            onElement(i, elements[i]);
        }
    }

    public static IReadOnlyList<TokenKind> FindStopTokensForVariable(IReadOnlyList<Element> elements, int variableIndex)
    {
        var stopTokens = new List<TokenKind>();
        for (var i = variableIndex + 1; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element is LiteralChunk literal)
            {
                foreach (var lit in literal.Value)
                {
                    if (lit is PunctuationLiteral punc)
                    {
                        stopTokens.Add(punc.TokenKind);
                    }
                    else if (lit is KeywordLiteral)
                    {
                        stopTokens.Add(TokenKind.Identifier);
                    }
                }

                if (stopTokens.Count > 0)
                {
                    break;
                }
            }
            else if (element is VariableChunk || element is DirectiveChunk)
            {
                break;
            }
        }

        return stopTokens;
    }

    public static IReadOnlyList<TokenKind> FindNextPunctuationDelimiters(int currentIndex, IReadOnlyList<Element> elements)
    {
        for (var i = currentIndex + 1; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element is LiteralChunk literalChunk)
            {
                foreach (var lit in literalChunk.Value)
                {
                    if (lit is PunctuationLiteral punc)
                    {
                        return new[] { punc.TokenKind };
                    }
                }

                continue;
            }

            if (element is AttrDictDirectiveChunk || element is AttrDictWithKeywordDirectiveChunk || element is PropDictDirectiveChunk)
            {
                return new[] { TokenKind.LBrace };
            }
        }

        return Array.Empty<TokenKind>();
    }

    public static int CountFieldsForElement(Element element)
    {
        switch (element)
        {
            case LiteralChunk literal:
            {
                var n = 0;
                foreach (var lit in literal.Value)
                {
                    if (lit is PunctuationLiteral || lit is KeywordLiteral)
                    {
                        n++;
                    }
                }

                return n;
            }

            case VariableChunk _: return 1;
            case AttrDictDirectiveChunk _: return 1;
            case AttrDictWithKeywordDirectiveChunk _: return 1;
            case PropDictDirectiveChunk _: return 1;
            case TypeDirectiveChunk _: return 1;
            case QualifiedDirectiveChunk _: return 1;
            case ResultsDirectiveChunk _: return 1;
            case FunctionalTypeDirectiveChunk _: return 1;
            case RegionsDirectiveChunk _: return 1;
            case SuccessorsDirectiveChunk _: return 1;
            case OperandsDirectiveChunk _: return 1;
            case OptionalGroup group:
                return CountFields(group.ThenElements) +
                       (group.ElseElements != null ? CountFields(group.ElseElements) : 0);
            case OilistDirectiveChunk oilist:
                return CountFieldsInOilist(oilist);
            default: return 0;
        }
    }

    public static int CountFields(IReadOnlyList<Element> elements)
    {
        var count = 0;
        foreach (var element in elements)
        {
            count += CountFieldsForElement(element);
        }

        return count;
    }

    public static int CountFields(IReadOnlyList<OilistElement> elements)
    {
        var count = 0;
        foreach (var element in elements)
        {
            count += element switch
            {
                OilistVariableElement _ => 1,
                OilistTypeDirectiveElement _ => 1,
                OilistLiteralElement _ => 1,
                _ => 0,
            };
        }

        return count;
    }

    public static int CountFieldsInOilist(OilistDirectiveChunk oilist)
    {
        var total = 0;
        foreach (var clause in oilist.Clauses)
        {
            total += 1 + CountFields(clause.Elements);
        }

        return total;
    }
}
