namespace TableGen;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// Processes C-style preprocessor directives in TableGen source files.
/// </summary>
/// <remarks>
/// The following directives are supported:
/// <list type="bullet">
/// <item><description><c>#define SYMBOL</c> — adds SYMBOL to the defines set</description></item>
/// <item><description><c>#ifndef SYMBOL</c> — includes content if SYMBOL is not defined</description></item>
/// <item><description><c>#ifdef SYMBOL</c> — includes content if SYMBOL is defined</description></item>
/// <item><description><c>#else</c> — switches the active branch of a conditional</description></item>
/// <item><description><c>#endif</c> — closes a conditional block</description></item>
/// </list>
/// Inactive lines are replaced with blank lines to preserve line numbers for diagnostics.
/// The <see cref="Process"/> method's <c>defines</c> parameter is updated in-place so that symbols defined in one file
/// remain visible when other files are processed.
/// </remarks>
public static class TableGenPreprocessor
{
    /// <summary>
    /// Processes preprocessor directives in <paramref name="source"/> using the given
    /// shared <paramref name="defines"/> set.
    /// </summary>
    /// <param name="source">The source text to process.</param>
    /// <param name="defines">
    /// The set of currently defined preprocessor symbols.  Updated in-place when
    /// <c>#define</c> directives are encountered in active regions.
    /// </param>
    /// <returns>
    /// The source text with inactive regions replaced by blank lines and preprocessor
    /// directive lines removed.
    /// </returns>
    public static string Process(string source, ISet<string> defines)
    {
        var output = new StringBuilder(source.Length);

        // Stack of (ownActive, parentActive) pairs.
        // ownActive:    whether this block's own condition is true.
        // parentActive: whether the enclosing context was active when this block was entered.
        // A region is included only when both ownActive && parentActive are true for
        // every frame on the stack.
        var condStack = new Stack<(bool ownActive, bool parentActive)>();

        bool IsActive()
        {
            foreach (var (ownActive, parentActive) in condStack)
            {
                if (!ownActive || !parentActive)
                {
                    return false;
                }
            }

            return true;
        }

        var lines = source.Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            // Normalize \r\n line endings.
            if (rawLine.Length > 0 && rawLine[rawLine.Length - 1] == '\r')
            {
                rawLine = rawLine.Substring(0, rawLine.Length - 1);
            }

            var trimmed = rawLine.TrimStart();

            if (trimmed.StartsWith("#ifndef ") || trimmed.StartsWith("#ifndef\t"))
            {
                var symbol = trimmed.Substring("#ifndef".Length).Trim();
                var parentActive = IsActive();
                condStack.Push((parentActive && !defines.Contains(symbol), parentActive));
                output.Append('\n');
                continue;
            }

            if (trimmed.StartsWith("#ifdef ") || trimmed.StartsWith("#ifdef\t"))
            {
                var symbol = trimmed.Substring("#ifdef".Length).Trim();
                var parentActive = IsActive();
                condStack.Push((parentActive && defines.Contains(symbol), parentActive));
                output.Append('\n');
                continue;
            }

            if (IsEndif(trimmed))
            {
                if (condStack.Count > 0)
                {
                    condStack.Pop();
                }

                output.Append('\n');
                continue;
            }

            if (IsElse(trimmed))
            {
                if (condStack.Count > 0)
                {
                    var (ownActive, parentActive) = condStack.Pop();
                    // Simply flip the own-active state; the parent context stays the same.
                    condStack.Push((!ownActive, parentActive));
                }

                output.Append('\n');
                continue;
            }

            if (trimmed.StartsWith("#define ") || trimmed.StartsWith("#define\t"))
            {
                if (IsActive())
                {
                    var rest = trimmed.Substring("#define".Length).Trim();
                    // Accept only the symbol name; ignore any replacement value.
                    var spaceIdx = IndexOfWhitespace(rest);
                    var symbol = spaceIdx >= 0 ? rest.Substring(0, spaceIdx) : rest;
                    if (symbol.Length > 0)
                    {
                        defines.Add(symbol);
                    }
                }

                output.Append('\n');
                continue;
            }

            if (trimmed.StartsWith("#"))
            {
                // Unknown or unsupported preprocessor directive – skip the line.
                output.Append('\n');
                continue;
            }

            // Ordinary source line: emit when active, blank line when inactive.
            if (IsActive())
            {
                output.Append(rawLine);
            }

            output.Append('\n');
        }

        return output.ToString();
    }

    private static bool IsEndif(string trimmed)
    {
        if (!trimmed.StartsWith("#endif"))
        {
            return false;
        }

        return trimmed.Length == "#endif".Length
            || char.IsWhiteSpace(trimmed["#endif".Length])
            || trimmed["#endif".Length] == '/';
    }

    private static bool IsElse(string trimmed)
    {
        if (!trimmed.StartsWith("#else"))
        {
            return false;
        }

        return trimmed.Length == "#else".Length
            || char.IsWhiteSpace(trimmed["#else".Length])
            || trimmed["#else".Length] == '/';
    }

    private static int IndexOfWhitespace(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsWhiteSpace(s[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
