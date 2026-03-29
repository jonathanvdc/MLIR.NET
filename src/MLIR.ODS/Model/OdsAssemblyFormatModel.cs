namespace MLIR.ODS.Model;

/// <summary>
/// Represents a declarative MLIR ODS assembly format.
/// </summary>
public sealed class OdsAssemblyFormatModel
{
    /// <summary>
    /// The sequence of elements that make up the assembly format.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement> Elements { get; }

    /// <summary>
    /// Creates a new assembly format model.
    /// </summary>
    public OdsAssemblyFormatModel(IReadOnlyList<OdsAssemblyFormatElement> elements)
    {
        Elements = elements;
    }
}

/// <summary>
/// Represents a single element in the assembly format (either a chunk or a group).
/// </summary>
public abstract class OdsAssemblyFormatElement
{
}

/// <summary>
/// Represents a non-group element such as a literal, variable, or directive.
/// </summary>
public abstract class OdsAssemblyFormatChunk : OdsAssemblyFormatElement
{
}

/// <summary>
/// A keyword, punctuation token, or whitespace literal surrounded by backticks.
/// Examples: `(`, `,`, `->`, `\n`, `foo`.
/// </summary>
public sealed class OdsAssemblyFormatLiteralChunk : OdsAssemblyFormatChunk
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a literal chunk.
    /// </summary>
    public OdsAssemblyFormatLiteralChunk(string value)
    {
        Value = value;
    }
}

/// <summary>
/// A variable reference such as $operand, $attr, $region, or $result.
/// </summary>
public sealed class OdsAssemblyFormatVariableChunk : OdsAssemblyFormatChunk
{
    /// <summary>
    /// The name of the referenced variable (without the leading '$').
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True when the variable is marked as the anchor of an optional group using ^.
    /// </summary>
    public bool IsAnchor { get; }

    /// <summary>
    /// Creates a variable reference.
    /// </summary>
    public OdsAssemblyFormatVariableChunk(string name, bool isAnchor = false)
    {
        Name = name;
        IsAnchor = isAnchor;
    }
}

/// <summary>
/// An optional group of the form:
/// ( then-elements )?
/// or
/// ( then-elements ):( else-elements )?
/// </summary>
public sealed class OdsAssemblyFormatOptionalGroup : OdsAssemblyFormatElement
{
    /// <summary>
    /// The anchor variable that controls whether the optional group is printed.
    /// This must correspond to a variable marked with '^' inside ThenElements.
    /// </summary>
    public string AnchorName { get; }

    /// <summary>
    /// Elements that are printed when the anchor is present.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement> ThenElements { get; }

    /// <summary>
    /// Elements that are printed when the anchor is absent (if any).
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement>? ElseElements { get; }

    /// <summary>
    /// Creates an optional group.
    /// </summary>
    public OdsAssemblyFormatOptionalGroup(
        string anchorName,
        IReadOnlyList<OdsAssemblyFormatElement> thenElements,
        IReadOnlyList<OdsAssemblyFormatElement>? elseElements = null)
    {
        AnchorName = anchorName;
        ThenElements = thenElements;
        ElseElements = elseElements;
    }
}

/// <summary>
/// Base type for builtin and custom directives.
/// </summary>
public abstract class OdsAssemblyFormatDirectiveChunk : OdsAssemblyFormatChunk
{
}

/// <summary>
/// Represents the 'attr-dict' directive.
/// </summary>
public sealed class OdsAssemblyFormatAttrDictDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'attr-dict-with-keyword' directive.
/// </summary>
public sealed class OdsAssemblyFormatAttrDictWithKeywordDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'prop-dict' directive.
/// </summary>
public sealed class OdsAssemblyFormatPropDictDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'operands' directive.
/// </summary>
public sealed class OdsAssemblyFormatOperandsDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'results' directive.
/// </summary>
public sealed class OdsAssemblyFormatResultsDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'regions' directive.
/// </summary>
public sealed class OdsAssemblyFormatRegionsDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// Represents the 'successors' directive.
/// </summary>
public sealed class OdsAssemblyFormatSuccessorsDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
}

/// <summary>
/// type(input)
/// </summary>
public sealed class OdsAssemblyFormatTypeDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatTypeDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// functional-type(inputs, outputs)
/// </summary>
public sealed class OdsAssemblyFormatFunctionalTypeDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The input operands passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Inputs { get; }

    /// <summary>
    /// The output operands passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Outputs { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatFunctionalTypeDirectiveChunk(
        OdsAssemblyFormatDirectiveOperand inputs,
        OdsAssemblyFormatDirectiveOperand outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}

/// <summary>
/// qualified(input)
/// </summary>
public sealed class OdsAssemblyFormatQualifiedDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatQualifiedDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// ref(input)
/// </summary>
public sealed class OdsAssemblyFormatRefDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatRefDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// custom&lt;Name&gt;(params...)
/// Params may be variables, type(...) directives, attr-dict / prop-dict,
/// string literals of C++ code, and ref(...) wrappers.
/// </summary>
public sealed class OdsAssemblyFormatCustomDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The name of the custom directive.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The parameters passed to the custom directive.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatDirectiveOperand> Parameters { get; }

    /// <summary>
    /// Creates the custom directive.
    /// </summary>
    public OdsAssemblyFormatCustomDirectiveChunk(
        string name,
        IReadOnlyList<OdsAssemblyFormatDirectiveOperand> parameters)
    {
        Name = name;
        Parameters = parameters;
    }
}

/// <summary>
/// oilist(`keyword` elements | `otherKeyword` elements ...)
/// </summary>
public sealed class OdsAssemblyFormatOilistDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The clauses that make up the oilist directive.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatOilistClause> Clauses { get; }

    /// <summary>
    /// Creates the oilist directive.
    /// </summary>
    public OdsAssemblyFormatOilistDirectiveChunk(IReadOnlyList<OdsAssemblyFormatOilistClause> clauses)
    {
        Clauses = clauses;
    }
}

/// <summary>
/// A single clause in an oilist directive.
/// </summary>
public sealed class OdsAssemblyFormatOilistClause
{
    /// <summary>
    /// The keyword that triggers this clause.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Elements printed when this clause is selected.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatOilistElement> Elements { get; }

    /// <summary>
    /// Creates an oilist clause.
    /// </summary>
    public OdsAssemblyFormatOilistClause(
        string keyword,
        IReadOnlyList<OdsAssemblyFormatOilistElement> elements)
    {
        Keyword = keyword;
        Elements = elements;
    }
}

/// <summary>
/// oilist elements are restricted to literals, variables, and type directives.
/// </summary>
public abstract class OdsAssemblyFormatOilistElement
{
}

/// <summary>
/// A literal element in an oilist clause.
/// </summary>
public sealed class OdsAssemblyFormatOilistLiteralElement : OdsAssemblyFormatOilistElement
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a literal oilist element.
    /// </summary>
    public OdsAssemblyFormatOilistLiteralElement(string value)
    {
        Value = value;
    }
}

/// <summary>
/// A variable element in an oilist clause.
/// </summary>
public sealed class OdsAssemblyFormatOilistVariableElement : OdsAssemblyFormatOilistElement
{
    /// <summary>
    /// The name of the referenced variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a variable oilist element.
    /// </summary>
    public OdsAssemblyFormatOilistVariableElement(string name)
    {
        Name = name;
    }
}

/// <summary>
/// A type directive element in an oilist clause.
/// </summary>
public sealed class OdsAssemblyFormatOilistTypeDirectiveElement : OdsAssemblyFormatOilistElement
{
    /// <summary>
    /// The operand passed to the type directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive oilist element.
    /// </summary>
    public OdsAssemblyFormatOilistTypeDirectiveElement(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// Base type for operands passed to directives.
/// </summary>
public abstract class OdsAssemblyFormatDirectiveOperand
{
}

/// <summary>
/// A variable reference used as a directive operand.
/// </summary>
public sealed class OdsAssemblyFormatVariableOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The name of the referenced variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a variable operand.
    /// </summary>
    public OdsAssemblyFormatVariableOperand(string name)
    {
        Name = name;
    }
}

/// <summary>
/// A nested type(...) directive used as an operand to another directive,
/// e.g. qualified(type($results)).
/// </summary>
public sealed class OdsAssemblyFormatTypeDirectiveOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The operand passed to the nested type directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive operand.
    /// </summary>
    public OdsAssemblyFormatTypeDirectiveOperand(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// A reference wrapper ref(...), typically passed to custom directives.
/// </summary>
public sealed class OdsAssemblyFormatRefDirectiveOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The operand wrapped by the ref directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a ref directive operand.
    /// </summary>
    public OdsAssemblyFormatRefDirectiveOperand(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}

/// <summary>
/// attr-dict used as a custom directive parameter.
/// </summary>
public sealed class OdsAssemblyFormatAttrDictOperand : OdsAssemblyFormatDirectiveOperand
{
}

/// <summary>
/// prop-dict used as a custom directive parameter.
/// </summary>
public sealed class OdsAssemblyFormatPropDictOperand : OdsAssemblyFormatDirectiveOperand
{
}

/// <summary>
/// A raw C++ expression/string literal passed to a custom directive parameter.
/// </summary>
public sealed class OdsAssemblyFormatCodeOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The raw C++ code string.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a code operand.
    /// </summary>
    public OdsAssemblyFormatCodeOperand(string code)
    {
        Code = code;
    }
}
