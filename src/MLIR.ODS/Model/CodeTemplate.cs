namespace MLIR.ODS.Model;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// A structured code snippet that wraps a C# code fragment and provides
/// canonical placeholder handling.
/// </summary>
/// <remarks>
/// <para>
/// Placeholders use the syntax <c>${token}</c> where <c>token</c> is either an
/// identifier composed of letters, digits, and underscores, or a decimal index.
/// The canonical named placeholders used by the generator are:
/// </para>
/// <list type="bullet">
///   <item><c>${parser}</c> – the <c>AttributeParsingContext</c> or equivalent parsing object.</item>
///   <item><c>${self}</c> – the storage value or source property being converted.</item>
///   <item><c>${syntax}</c> – the per-parameter <c>AttributeValueSyntax</c> variable.</item>
///   <item><c>${value}</c> – the typed value being encoded into storage (e.g., for const-builder calls).</item>
///   <item><c>${context}</c> – an optional builder or printing context.</item>
/// </list>
/// <para>
/// Legacy <c>$_name</c>-style placeholders are normalized to canonical
/// <c>${name}</c> form by <see cref="From(string, CodeTemplateKind, IReadOnlyDictionary{string, string}?)"/>,
/// and legacy positional placeholders of the form <c>$N</c> are normalized to
/// canonical <c>${N}</c> form. Callers may also rename normalized placeholder
/// tokens during import when a different canonical vocabulary is desired.
/// </para>
/// <para>
/// This type intentionally does not parse or validate C# syntax beyond placeholder
/// substitution.
/// </para>
/// </remarks>
public sealed class CodeTemplate
{
    // Matches canonical placeholders of the form ${name} or ${N}.
    private static readonly Regex PlaceholderRegex =
        new Regex(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*|[0-9]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches legacy $_name placeholders and captures the bare placeholder name.
    private static readonly Regex LegacyPlaceholderRegex =
        new Regex(@"\$_([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches legacy positional placeholders like $0, $1, and $23.
    private static readonly Regex LegacyPositionalPlaceholderRegex =
        new Regex(@"\$([0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, string> EmptyRenameMap =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new <see cref="CodeTemplate"/> with the given text and kind.
    /// </summary>
    /// <param name="text">
    /// The template text, which may contain canonical placeholders such as
    /// <c>${name}</c> or <c>${0}</c>.
    /// </param>
    /// <param name="kind">
    /// The structural kind of the snippet (for documentation purposes only).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    public CodeTemplate(string text, CodeTemplateKind kind)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Kind = kind;
        PlaceholderNames = ExtractPlaceholderNames(text);
    }

    /// <summary>
    /// Gets the raw template text, including any canonical placeholders.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the structural kind of this code snippet.
    /// </summary>
    public CodeTemplateKind Kind { get; }

    /// <summary>
    /// Gets the ordered, deduplicated list of placeholder tokens found in <see cref="Text"/>.
    /// Each entry is the bare identifier or decimal index without the surrounding
    /// <c>${…}</c> delimiters.
    /// </summary>
    public IReadOnlyList<string> PlaceholderNames { get; }

    /// <summary>
    /// Returns a copy of <see cref="Text"/> with all canonical placeholders replaced by
    /// the corresponding values from <paramref name="values"/>.
    /// </summary>
    /// <param name="values">
    /// A dictionary mapping placeholder tokens to their replacement strings.
    /// Every placeholder token present in <see cref="PlaceholderNames"/> must have an entry;
    /// additional entries are silently ignored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a placeholder in the template has no corresponding entry in
    /// <paramref name="values"/>.
    /// </exception>
    public string Render(IReadOnlyDictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return PlaceholderRegex.Replace(Text, match =>
        {
            var name = match.Groups[1].Value;
            if (!values.TryGetValue(name, out var replacement))
            {
                throw new InvalidOperationException(
                    "CodeTemplate is missing a value for placeholder '${" + name + "}'. "
                    + "Template text: " + Text);
            }

            return replacement;
        });
    }

    /// <summary>
    /// Renders this template by providing a single placeholder token and value.
    /// </summary>
    /// <param name="name">
    /// The placeholder token to replace.
    /// </param>
    /// <param name="value">
    /// The replacement string for the placeholder.
    /// </param>
    /// <returns>
    /// The rendered template text.
    /// </returns>
    /// <remarks>
    /// This is a convenience overload equivalent to calling <see cref="Render(IReadOnlyDictionary{string, string})"/>
    /// with a dictionary containing a single entry.
    /// </remarks>
    public string Render(string name, string value)
    {
        return Render(new Dictionary<string, string>(1, StringComparer.Ordinal)
        {
            [name] = value
        });
    }

    /// <summary>
    /// Renders this template by providing multiple placeholder token/value pairs.
    /// </summary>
    /// <param name="first">
    /// The first placeholder token/value pair.
    /// </param>
    /// <param name="rest">
    /// Additional placeholder token/value pairs.
    /// </param>
    /// <returns>
    /// The rendered template text.
    /// </returns>
    /// <remarks>
    /// This is a convenience overload equivalent to calling <see cref="Render(IReadOnlyDictionary{string, string})"/>
    /// with a dictionary constructed from the provided pairs. If duplicate tokens are provided,
    /// the last value wins.
    /// </remarks>
    public string Render((string Name, string Value) first, params (string Name, string Value)[] rest)
    {
        var values = new Dictionary<string, string>(1 + rest.Length, StringComparer.Ordinal)
        {
            [first.Name] = first.Value
        };

        foreach (var (name, value) in rest)
        {
            values[name] = value;
        }

        return Render(values);
    }

    /// <summary>
    /// Validates that every placeholder in this template belongs to
    /// <paramref name="allowedPlaceholders"/>.
    /// </summary>
    /// <param name="allowedPlaceholders">
    /// The set of placeholder tokens that are valid at this use site.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the template contains a placeholder whose name is not in
    /// <paramref name="allowedPlaceholders"/>.
    /// </exception>
    public void RequireOnly(params string[] allowedPlaceholders)
    {
        if (allowedPlaceholders == null)
        {
            throw new ArgumentNullException(nameof(allowedPlaceholders));
        }

        var allowed = new HashSet<string>(allowedPlaceholders, StringComparer.Ordinal);
        foreach (var name in PlaceholderNames)
        {
            if (!allowed.Contains(name))
            {
                throw new InvalidOperationException(
                    "CodeTemplate contains an unsupported placeholder '${" + name + "}'. "
                    + "Allowed placeholders at this use site: "
                    + (allowedPlaceholders.Length == 0 ? "(none)" : string.Join(", ", allowedPlaceholders))
                    + ". Template text: " + Text);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="CodeTemplate"/> from a raw string that may use either legacy
    /// <c>$_name</c>-style placeholders and legacy positional placeholders of the form
    /// <c>$N</c>, or canonical placeholders of the form <c>${name}</c> and <c>${N}</c>.
    /// </summary>
    /// <param name="text">
    /// The raw template text, or <see langword="null"/>. When <see langword="null"/>,
    /// this method returns <see langword="null"/>.
    /// </param>
    /// <param name="kind">
    /// The structural kind to assign to the resulting template.
    /// </param>
    /// <param name="renames">
    /// An optional mapping from normalized placeholder tokens to replacement canonical
    /// tokens. For example, a mapping of <c>"0"</c> to <c>"value"</c> rewrites
    /// <c>$0</c> or <c>${0}</c> to <c>${value}</c> during import.
    /// </param>
    /// <returns>
    /// A <see cref="CodeTemplate"/> with all legacy placeholder spellings normalized to
    /// canonical placeholder form and then renamed according to <paramref name="renames"/>,
    /// or <see langword="null"/> when <paramref name="text"/> is <see langword="null"/>
    /// or empty.
    /// </returns>
    public static CodeTemplate? From(
        string? text,
        CodeTemplateKind kind,
        IReadOnlyDictionary<string, string>? renames = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var normalized = NormalizeLegacyPlaceholders(text!, renames ?? EmptyRenameMap);
        return new CodeTemplate(normalized, kind);
    }

    /// <summary>
    /// Replaces legacy placeholders in <paramref name="text"/> with their canonical
    /// placeholder equivalents, then applies any requested token renames.
    /// </summary>
    /// <remarks>
    /// Any legacy placeholder of the form <c>$_name</c> is rewritten to <c>${name}</c>.
    /// Any legacy positional placeholder of the form <c>$N</c> is rewritten to <c>${N}</c>.
    /// After normalization, canonical placeholders are rewritten according to
    /// <paramref name="renames"/>.
    /// </remarks>
    private static string NormalizeLegacyPlaceholders(
        string text,
        IReadOnlyDictionary<string, string> renames)
    {
        text = LegacyPlaceholderRegex.Replace(text, match => "${" + match.Groups[1].Value + "}");
        text = LegacyPositionalPlaceholderRegex.Replace(text, match => "${" + match.Groups[1].Value + "}");

        if (renames.Count == 0)
        {
            return text;
        }

        return PlaceholderRegex.Replace(text, match =>
        {
            var token = match.Groups[1].Value;
            return renames.TryGetValue(token, out var renamedToken)
                ? "${" + renamedToken + "}"
                : match.Value;
        });
    }

    /// <summary>
    /// Extracts an ordered, deduplicated list of placeholder tokens from <paramref name="text"/>.
    /// </summary>
    private static IReadOnlyList<string> ExtractPlaceholderNames(string text)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var names = new List<string>();

        foreach (Match match in PlaceholderRegex.Matches(text))
        {
            var name = match.Groups[1].Value;
            if (seen.Add(name))
            {
                names.Add(name);
            }
        }

        return names.AsReadOnly();
    }
}
