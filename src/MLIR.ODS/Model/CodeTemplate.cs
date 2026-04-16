namespace MLIR.ODS.Model;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// A structured code snippet that wraps a C# code fragment and provides
/// canonical <c>${name}</c>-style placeholder handling.
/// </summary>
/// <remarks>
/// <para>
/// Placeholders use the syntax <c>${name}</c> where <c>name</c> is an identifier
/// composed of letters, digits, and underscores. The canonical placeholder names
/// used by the generator are:
/// </para>
/// <list type="bullet">
///   <item><c>${parser}</c> – the <c>AttributeParsingContext</c> or equivalent parsing object.</item>
///   <item><c>${self}</c> – the storage value or source property being converted.</item>
///   <item><c>${syntax}</c> – the per-parameter <c>AttributeValueSyntax</c> variable.</item>
///   <item><c>${value}</c> – the typed value being encoded into storage (e.g., for const-builder calls).</item>
///   <item><c>${context}</c> – an optional builder or printing context.</item>
/// </list>
/// <para>
/// Legacy placeholder spellings (<c>$_parser</c>, <c>$_self</c>, <c>$_syntax</c>, <c>$0</c>)
/// are normalized to the canonical form by <see cref="FromLegacy(string, CodeTemplateKind)"/>
/// so that emitters never need to handle both spellings.
/// </para>
/// <para>
/// This type intentionally does not parse or validate C# syntax beyond placeholder
/// substitution.
/// </para>
/// </remarks>
public sealed class CodeTemplate
{
    // Matches ${name} placeholders. The name must start with a letter or underscore.
    private static readonly Regex PlaceholderRegex =
        new Regex(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Legacy-to-canonical placeholder mapping.
    private static readonly (string Legacy, string Canonical)[] LegacyMappings =
    {
        ("$_parser", "${parser}"),
        ("$_self",   "${self}"),
        ("$_syntax", "${syntax}"),
        ("$0",       "${value}"),
    };

    /// <summary>
    /// Initializes a new <see cref="CodeTemplate"/> with the given text and kind.
    /// </summary>
    /// <param name="text">
    /// The template text, which may contain <c>${name}</c> placeholders.
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
    /// Gets the raw template text, including any <c>${name}</c> placeholders.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the structural kind of this code snippet.
    /// </summary>
    public CodeTemplateKind Kind { get; }

    /// <summary>
    /// Gets the ordered, deduplicated list of placeholder names found in <see cref="Text"/>.
    /// Each entry is the bare name without the surrounding <c>${…}</c> delimiters.
    /// </summary>
    public IReadOnlyList<string> PlaceholderNames { get; }

    /// <summary>
    /// Returns a copy of <see cref="Text"/> with all <c>${name}</c> placeholders replaced by
    /// the corresponding values from <paramref name="values"/>.
    /// </summary>
    /// <param name="values">
    /// A dictionary mapping placeholder names to their replacement strings.
    /// Every placeholder name present in <see cref="PlaceholderNames"/> must have an entry;
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
    /// Validates that every placeholder in this template belongs to
    /// <paramref name="allowedPlaceholders"/>.
    /// </summary>
    /// <param name="allowedPlaceholders">
    /// The set of placeholder names that are valid at this use site.
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
    /// placeholder spellings (<c>$_parser</c>, <c>$_self</c>, <c>$_syntax</c>, <c>$0</c>)
    /// or the canonical <c>${name}</c> syntax.
    /// </summary>
    /// <param name="text">
    /// The raw template text, or <see langword="null"/>. When <see langword="null"/>,
    /// this method returns <see langword="null"/>.
    /// </param>
    /// <param name="kind">
    /// The structural kind to assign to the resulting template.
    /// </param>
    /// <returns>
    /// A <see cref="CodeTemplate"/> with all legacy placeholder spellings normalized to
    /// <c>${name}</c> form, or <see langword="null"/> when <paramref name="text"/> is
    /// <see langword="null"/> or empty.
    /// </returns>
    public static CodeTemplate? FromLegacy(string? text, CodeTemplateKind kind)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var normalized = NormalizeLegacyPlaceholders(text!);
        return new CodeTemplate(normalized, kind);
    }

    /// <summary>
    /// Replaces all known legacy placeholder spellings in <paramref name="text"/> with their
    /// canonical <c>${name}</c> equivalents.
    /// </summary>
    /// <remarks>
    /// Applies replacements in declaration order to avoid double-substitution. The mappings are:
    /// <list type="bullet">
    ///   <item><c>$_parser</c> → <c>${parser}</c></item>
    ///   <item><c>$_self</c>   → <c>${self}</c></item>
    ///   <item><c>$_syntax</c> → <c>${syntax}</c></item>
    ///   <item><c>$0</c>       → <c>${value}</c></item>
    /// </list>
    /// </remarks>
    private static string NormalizeLegacyPlaceholders(string text)
    {
        foreach (var (legacy, canonical) in LegacyMappings)
        {
            text = text.Replace(legacy, canonical);
        }

        return text;
    }

    /// <summary>
    /// Extracts an ordered, deduplicated list of placeholder names from <paramref name="text"/>.
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
