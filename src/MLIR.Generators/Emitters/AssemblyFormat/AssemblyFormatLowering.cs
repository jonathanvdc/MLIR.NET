namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Shared lowering entry points for declarative assembly formats.
/// </summary>
/// <remarks>
/// The lowering layer owns the common walk over ODS assembly-format elements and
/// produces the stable, ordered representation consumed by syntax-class,
/// parse, bind, build, and write emitters. Domain-specific callers still decide
/// how to emit final C# statements from the lowered slots, but they no longer
/// rediscover the format shape independently.
/// </remarks>
internal static class AssemblyFormatLowerer
{
    public static LoweredAssemblyFormat LowerAttribute(AttributeModel attribute, AssemblyFormatModel format)
    {
        var sink = new AttrOrTypeFormatSink(attribute.Parameters, includeTrivia: true);
        LowerElements(format.Elements, sink);
        return new LoweredAssemblyFormat(sink.Slots);
    }

    public static LoweredAssemblyFormat LowerType(TypeModel type, AssemblyFormatModel format)
    {
        var sink = new AttrOrTypeFormatSink(type.Parameters, includeTrivia: false);
        LowerElements(format.Elements, sink);
        return new LoweredAssemblyFormat(sink.Slots);
    }

    public static LoweredOperationAssemblyFormat LowerOperation(OperationModel operation, AssemblyFormatModel format)
    {
        var sink = new OperationFormatSink(operation);
        LowerElements(format.Elements, sink);
        return new LoweredOperationAssemblyFormat(sink.Elements, sink.Metadata);
    }

    private static void LowerElements(IReadOnlyList<Element> elements, IAssemblyFormatLoweringSink sink)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            LowerElement(elements[i], i, sink);
        }
    }

    private static void LowerElement(Element element, int elementIndex, IAssemblyFormatLoweringSink sink)
    {
        switch (element)
        {
            case LiteralChunk literal:
                sink.LowerLiteral(literal, elementIndex);
                break;
            case VariableChunk variable:
                sink.LowerVariable(variable, elementIndex);
                break;
            case OptionalGroup optionalGroup:
                sink.LowerOptionalGroup(optionalGroup, elementIndex);
                break;
            case OilistDirectiveChunk oilist:
                sink.LowerOilist(oilist, elementIndex);
                break;
            case DirectiveChunk directive:
                sink.LowerDirective(directive, elementIndex);
                break;
            default:
                sink.LowerUnsupported(element, elementIndex);
                break;
        }
    }

    public static OperationFormatElementKind GetOperationElementKind(Element element)
    {
        return element switch
        {
            LiteralChunk _ => OperationFormatElementKind.Literal,
            VariableChunk _ => OperationFormatElementKind.Variable,
            AttrDictDirectiveChunk _ => OperationFormatElementKind.AttrDict,
            AttrDictWithKeywordDirectiveChunk _ => OperationFormatElementKind.AttrDictWithKeyword,
            PropDictDirectiveChunk _ => OperationFormatElementKind.PropDict,
            TypeDirectiveChunk _ => OperationFormatElementKind.Type,
            QualifiedDirectiveChunk _ => OperationFormatElementKind.QualifiedType,
            ResultsDirectiveChunk _ => OperationFormatElementKind.ResultsType,
            FunctionalTypeDirectiveChunk _ => OperationFormatElementKind.FunctionalType,
            RegionsDirectiveChunk _ => OperationFormatElementKind.Regions,
            SuccessorsDirectiveChunk _ => OperationFormatElementKind.Successors,
            OperandsDirectiveChunk _ => OperationFormatElementKind.Operands,
            OptionalGroup _ => OperationFormatElementKind.OptionalGroup,
            OilistDirectiveChunk _ => OperationFormatElementKind.Oilist,
            _ => OperationFormatElementKind.Unsupported,
        };
    }

    private interface IAssemblyFormatLoweringSink
    {
        void LowerLiteral(LiteralChunk literal, int elementIndex);

        void LowerVariable(VariableChunk variable, int elementIndex);

        void LowerDirective(DirectiveChunk directive, int elementIndex);

        void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex);

        void LowerOilist(OilistDirectiveChunk oilist, int elementIndex);

        void LowerUnsupported(Element element, int elementIndex);
    }

    private sealed class AttrOrTypeFormatSink : IAssemblyFormatLoweringSink
    {
        private readonly IReadOnlyList<AttrOrTypeParameterModel> parameters;
        private readonly bool includeTrivia;
        private int literalIndex;

        public AttrOrTypeFormatSink(IReadOnlyList<AttrOrTypeParameterModel> parameters, bool includeTrivia)
        {
            this.parameters = parameters;
            this.includeTrivia = includeTrivia;
            Slots = new List<FormatSlot>();
        }

        public List<FormatSlot> Slots { get; }

        public void LowerLiteral(LiteralChunk literal, int elementIndex)
        {
            foreach (var lit in literal.Value)
            {
                switch (lit)
                {
                    case PunctuationLiteral punc:
                        AddLiteralTokenSlot(EmitterHelpers.GetPunctuationText(punc.TokenKind), "TokenKind." + punc.TokenKind, isKeyword: false);
                        break;

                    case KeywordLiteral kw:
                        AddLiteralTokenSlot(kw.Spelling, "TokenKind.Identifier", isKeyword: true);
                        break;

                    case WhitespaceLiteral ws when includeTrivia:
                        Slots.Add(new TriviaSlot { Text = ws.Spaces, IsNewline = false });
                        break;

                    case NewlineLiteral when includeTrivia:
                        Slots.Add(new TriviaSlot { Text = "\n", IsNewline = true });
                        break;
                }
            }
        }

        public void LowerVariable(VariableChunk variable, int elementIndex)
        {
            var param = FindParameter(parameters, variable.Name);
            Slots.Add(new VariableSlot
            {
                Name = variable.Name,
                SyntaxType = GetResolvedCSharpSyntaxType(param),
                SyntaxShape = GetResolvedCSharpSyntaxShape(param),
                ParamModel = param,
            });
        }

        public void LowerDirective(DirectiveChunk directive, int elementIndex)
        {
        }

        public void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex)
        {
        }

        public void LowerOilist(OilistDirectiveChunk oilist, int elementIndex)
        {
        }

        public void LowerUnsupported(Element element, int elementIndex)
        {
        }

        private void AddLiteralTokenSlot(string syntheticText, string kindExpr, bool isKeyword)
        {
            Slots.Add(new LiteralTokenSlot
            {
                LocalName = "literal" + literalIndex + "Token",
                SyntheticText = syntheticText,
                KindExpr = kindExpr,
                IsKeyword = isKeyword,
            });
            literalIndex++;
        }
    }

    private sealed class OperationFormatSink : IAssemblyFormatLoweringSink
    {
        private readonly OperationModel operation;
        private readonly HashSet<string> usedNames;

        public OperationFormatSink(OperationModel operation)
        {
            this.operation = operation;
            Metadata = new OperationBodySyntaxMetadata(DialectGeneratorNaming.GetOperationClassName(operation));
            Elements = new List<LoweredOperationElement>();
            usedNames = new HashSet<string>(System.StringComparer.Ordinal);
        }

        public OperationBodySyntaxMetadata Metadata { get; }

        public List<LoweredOperationElement> Elements { get; }

        public void LowerLiteral(LiteralChunk literal, int elementIndex)
        {
            LowerSupportedOperationElement(literal, elementIndex);
        }

        public void LowerVariable(VariableChunk variable, int elementIndex)
        {
            LowerSupportedOperationElement(variable, elementIndex);
        }

        public void LowerDirective(DirectiveChunk directive, int elementIndex)
        {
            LowerSupportedOperationElement(directive, elementIndex);
        }

        public void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex)
        {
            LowerSupportedOperationElement(optionalGroup, elementIndex);
        }

        public void LowerOilist(OilistDirectiveChunk oilist, int elementIndex)
        {
            LowerSupportedOperationElement(oilist, elementIndex);
        }

        public void LowerUnsupported(Element element, int elementIndex)
        {
            AddElement(element, elementIndex, fieldStart: Metadata.Fields.Count, fieldCount: 0);
        }

        private void LowerSupportedOperationElement(Element element, int elementIndex)
        {
            var start = Metadata.Fields.Count;
            AppendBodySyntaxFields(element);
            AddElement(element, elementIndex, start, Metadata.Fields.Count - start);
        }

        private void AddElement(Element element, int elementIndex, int fieldStart, int fieldCount)
        {
            var kind = GetOperationElementKind(element);
            Elements.Add(new LoweredOperationElement(
                element,
                kind,
                elementIndex,
                fieldStart,
                fieldCount,
                kind != OperationFormatElementKind.Unsupported));
        }

        private void AppendBodySyntaxFields(Element element, bool nullable = false)
        {
            AssemblyFormatLowerer.AppendBodySyntaxFields(usedNames, element, operation, Metadata, nullable);
        }
    }

    private static void AppendBodySyntaxFields(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable = false)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                            AppendPunctuationField(usedNames, punc.TokenKind, metadata, nullable);
                            break;

                        case KeywordLiteral kw:
                            AppendKeywordField(usedNames, kw.Spelling, metadata, nullable);
                            break;
                    }
                }

                break;

            case VariableChunk variable:
                AppendVariableField(usedNames, variable.Name, operation, metadata, nullable);
                break;

            case AttrDictDirectiveChunk _:
                AppendDelimitedField(usedNames, metadata, "AttrDict", BodyComponentKind.AttrDict);
                break;

            case AttrDictWithKeywordDirectiveChunk _:
                AppendDelimitedField(usedNames, metadata, "AttrDictWithKeyword", BodyComponentKind.AttrDictWithKeyword);
                break;

            case PropDictDirectiveChunk _:
                AppendDelimitedField(usedNames, metadata, "PropDict", BodyComponentKind.PropDict);
                break;

            case RegionsDirectiveChunk _:
            {
                var name = EmitterHelpers.MakeUnique("Regions", usedNames);
                AddBodySyntaxField(
                    metadata,
                    BodyComponentKind.Regions,
                    "Regions",
                    name,
                    "IReadOnlyList<RegionSyntax>",
                    "foreach (var region in " + name + ")\n" +
                    "{\n" +
                    "    writer.WriteRegion(region);\n" +
                    "}");
                break;
            }

            case TypeDirectiveChunk typeDir:
                AppendTypeField(
                    usedNames,
                    GetTypeBaseName(typeDir.Operand),
                    EmitterHelpers.GetDirectiveOperandName(typeDir.Operand),
                    operation,
                    metadata,
                    nullable,
                    BodyComponentKind.TypeDirective);
                break;

            case SuccessorsDirectiveChunk _:
                AppendDelimitedField(usedNames, metadata, "Successors", BodyComponentKind.Successors, "DelimitedSyntaxList<Token>");
                break;

            case OperandsDirectiveChunk _:
                AppendDelimitedField(usedNames, metadata, "Operands", BodyComponentKind.Operands, "DelimitedSyntaxList<Token>");
                break;

            case QualifiedDirectiveChunk qualified:
                AppendTypeField(
                    usedNames,
                    GetQualifiedTypeBaseName(qualified.Operand),
                    EmitterHelpers.GetDirectiveOperandName(qualified.Operand),
                    operation,
                    metadata,
                    nullable,
                    BodyComponentKind.TypeDirective);
                break;

            case ResultsDirectiveChunk _:
                AppendTypeField(
                    usedNames,
                    "ResultsType",
                    "Results",
                    operation,
                    metadata,
                    nullable,
                    BodyComponentKind.ResultsDirective);
                break;

            case FunctionalTypeDirectiveChunk _:
                AppendTypeField(
                    usedNames,
                    "FunctionalType",
                    "Type",
                    operation,
                    metadata,
                    nullable,
                    BodyComponentKind.FunctionalTypeDirective);
                break;

            case OptionalGroup optionalGroup:
                foreach (var inner in optionalGroup.ThenElements)
                {
                    AppendBodySyntaxFields(usedNames, inner, operation, metadata, nullable: true);
                }

                if (optionalGroup.ElseElements != null)
                {
                    foreach (var inner in optionalGroup.ElseElements)
                    {
                        AppendBodySyntaxFields(usedNames, inner, operation, metadata, nullable: true);
                    }
                }

                break;

            case OilistDirectiveChunk oilist:
                foreach (var clause in oilist.Clauses)
                {
                    AppendKeywordField(usedNames, clause.Keyword, metadata, nullable: true, isOilistKeyword: true);

                    foreach (var oiElem in clause.Elements)
                    {
                        AppendOilistElementFields(usedNames, oiElem, operation, metadata);
                    }
                }

                break;
        }
    }

    private sealed class BodyFieldSpec
    {
        public BodyFieldSpec(
            BodyComponentKind componentKind,
            string componentName,
            string fieldBaseName,
            string csType,
            System.Func<string, string> writeStmtFactory)
        {
            ComponentKind = componentKind;
            ComponentName = componentName;
            FieldBaseName = fieldBaseName;
            CsType = csType;
            WriteStmtFactory = writeStmtFactory;
        }

        public BodyComponentKind ComponentKind { get; }

        public string ComponentName { get; }

        public string FieldBaseName { get; }

        public string CsType { get; }

        public System.Func<string, string> WriteStmtFactory { get; }
    }

    private static void AddBodySyntaxField(
        OperationBodySyntaxMetadata metadata,
        BodyComponentKind componentKind,
        string componentName,
        string name,
        string csType,
        string writeStmt)
    {
        var field = new BodySyntaxField(name, csType, writeStmt);
        metadata.AddField(field);
        metadata.AddComponentField(new BodyComponentField(componentKind, componentName, field.Name));
    }

    private static void AddBodySyntaxField(
        HashSet<string> usedNames,
        OperationBodySyntaxMetadata metadata,
        BodyFieldSpec spec)
    {
        var name = EmitterHelpers.MakeUnique(spec.FieldBaseName, usedNames);
        AddBodySyntaxField(metadata, spec.ComponentKind, spec.ComponentName, name, spec.CsType, spec.WriteStmtFactory(name));
    }

    private static (string CsType, string WriteStmt) GetTokenFieldShape(string name, string leadingTrivia, bool nullable)
    {
        return nullable
            ? ("Token?", "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \"" + leadingTrivia + "\");")
            : ("Token", "writer.WriteToken(" + name + ", \"" + leadingTrivia + "\");");
    }

    private static (string CsType, string WriteStmt) GetSyntaxNodeFieldShape(string name, string syntaxType, bool nullable)
    {
        return nullable
            ? (syntaxType + "?", "if (" + name + " != null) { writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer); }")
            : (syntaxType, "writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer);");
    }

    private static void AppendDelimitedField(
        HashSet<string> usedNames,
        OperationBodySyntaxMetadata metadata,
        string baseName,
        BodyComponentKind componentKind,
        string csType = "DelimitedSyntaxList<NamedAttributeSyntax>")
    {
        AddBodySyntaxField(
            usedNames,
            metadata,
            new BodyFieldSpec(
                componentKind,
                baseName,
                baseName,
                csType,
                name => string.Concat("writer.WriteDelimitedList(", name, ", ", '"', " ", '"', ");")));
    }

    private static void AppendOilistElementFields(HashSet<string> usedNames, OilistElement element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        switch (element)
        {
            case OilistVariableElement variable:
                AppendVariableField(usedNames, variable.Name, operation, metadata, nullable: true);
                break;

            case OilistTypeDirectiveElement typeDir:
                AppendTypeField(
                    usedNames,
                    GetTypeBaseName(typeDir.Operand),
                    EmitterHelpers.GetDirectiveOperandName(typeDir.Operand),
                    operation,
                    metadata,
                    nullable: true,
                    BodyComponentKind.TypeDirective);
                break;

            case OilistLiteralElement literal:
            {
                var name = EmitterHelpers.MakeUnique(DialectGeneratorNaming.ToPascalCase(literal.Value) + "Token", usedNames);
                AddBodySyntaxField(
                    metadata,
                    BodyComponentKind.Literal,
                    "OilistLiteral:" + literal.Value,
                    name,
                    "Token?",
                    "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value);");
                break;
            }
        }
    }

    private static void AppendPunctuationField(HashSet<string> usedNames, TokenKind tokenKind, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        var leadingTrivia = GetPunctuationLeadingTrivia(tokenKind);
        AddTokenBodyField(
            usedNames,
            metadata,
            BodyComponentKind.Literal,
            "Punctuation:" + tokenKind,
            GetPunctuationFieldName(tokenKind),
            leadingTrivia,
            nullable);
    }

    private static void AppendKeywordField(HashSet<string> usedNames, string spelling, OperationBodySyntaxMetadata metadata, bool nullable, bool isOilistKeyword = false)
    {
        var leadingTrivia = isOilistKeyword ? "\\n    " : " ";
        AddTokenBodyField(
            usedNames,
            metadata,
            BodyComponentKind.Literal,
            "Keyword:" + spelling,
            DialectGeneratorNaming.ToPascalCase(spelling) + "Keyword",
            leadingTrivia,
            nullable);
    }

    private static void AddTokenBodyField(
        HashSet<string> usedNames,
        OperationBodySyntaxMetadata metadata,
        BodyComponentKind componentKind,
        string componentName,
        string fieldBaseName,
        string leadingTrivia,
        bool nullable)
    {
        var csType = nullable ? "Token?" : "Token";
        AddBodySyntaxField(
            usedNames,
            metadata,
            new BodyFieldSpec(
                componentKind,
                componentName,
                fieldBaseName,
                csType,
                name => GetTokenWriteStmt(name, leadingTrivia, nullable)));
    }

    private static string GetTokenWriteStmt(string name, string leadingTrivia, bool nullable)
    {
        return nullable
            ? "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \"" + leadingTrivia + "\");"
            : "writer.WriteToken(" + name + ", \"" + leadingTrivia + "\");";
    }

    private static void AppendVariableField(HashSet<string> usedNames, string variableName, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        var name = DialectGeneratorNaming.ToPascalCase(variableName);
        if (TryAppendAttributeVariableField(variableName, name, operation, metadata, nullable))
        {
            return;
        }

        if (TryAppendRegionVariableField(variableName, name, operation, metadata, nullable))
        {
            return;
        }

        if (TryAppendVariadicOperandVariableField(variableName, name, operation, metadata))
        {
            return;
        }

        AppendTokenVariableField(usedNames, variableName, operation, metadata, nullable);
    }

    private static bool TryAppendAttributeVariableField(string variableName, string name, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        if (!EmitterHelpers.ContainsName(operation.Attributes, variableName, static attribute => attribute.Name))
        {
            return false;
        }

        var (csType, writeStmt) = GetSyntaxNodeFieldShape(name, "AttributeValueSyntax", nullable);
        AddBodySyntaxField(metadata, BodyComponentKind.Attribute, variableName, name, csType, writeStmt);
        return true;
    }

    private static bool TryAppendRegionVariableField(string variableName, string name, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        if (!EmitterHelpers.ContainsName(operation.Regions, variableName, static region => region.Name))
        {
            return false;
        }

        var (csType, writeStmt) = GetRegionFieldShape(name, nullable, IsVariadicRegion(operation, variableName));
        AddBodySyntaxField(
            metadata,
            EmitterHelpers.GetComponentKindForVariable(operation, variableName),
            variableName,
            name,
            csType,
            writeStmt);
        return true;
    }

    private static bool TryAppendVariadicOperandVariableField(string variableName, string name, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        if (!IsVariadicOperand(operation, variableName))
        {
            return false;
        }

        const string csType = "global::System.Collections.Generic.IReadOnlyList<Token>";
        var writeStmt =
            "for (var _i = 0; _i < " + name + ".Count; _i++) { " +
            "if (_i > 0) writer.WriteToken(TokenFactory.Comma(), \"\"); " +
            "writer.WriteToken(" + name + "[_i], \" \"); }";
        AddBodySyntaxField(
            metadata,
            EmitterHelpers.GetComponentKindForVariable(operation, variableName),
            variableName,
            name,
            csType,
            writeStmt);
        return true;
    }

    private static void AppendTokenVariableField(HashSet<string> usedNames, string variableName, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        AddTokenBodyField(
            usedNames,
            metadata,
            EmitterHelpers.GetComponentKindForVariable(operation, variableName),
            variableName,
            DialectGeneratorNaming.ToPascalCase(variableName),
            " ",
            nullable);
    }

    private static (string CsType, string WriteStmt) GetRegionFieldShape(string name, bool nullable, bool isVariadic)
    {
        if (isVariadic)
        {
            return (
                "global::System.Collections.Generic.IReadOnlyList<RegionSyntax>",
                "foreach (var region in " + name + ") { writer.WriteRegion(region); }");
        }

        return nullable
            ? ("RegionSyntax?", "if (" + name + ".HasValue) writer.WriteRegion(" + name + ".Value);")
            : ("RegionSyntax", "writer.WriteRegion(" + name + ");");
    }

    private static bool IsVariadicOperand(OperationModel operation, string variableName)
    {
        return ContainsVariadic(operation.Operands, variableName, static operand => operand.Name, static operand => operand.IsVariadic);
    }

    private static bool IsVariadicResult(OperationModel operation, string variableName)
    {
        return ContainsVariadic(operation.Results, variableName, static result => result.Name, static result => result.IsVariadic);
    }

    private static bool IsVariadicRegion(OperationModel operation, string variableName)
    {
        return ContainsVariadic(operation.Regions, variableName, static region => region.Name, static region => region.IsVariadic);
    }

    private static bool ContainsVariadic<T>(
        IEnumerable<T> items,
        string variableName,
        System.Func<T, string> getName,
        System.Func<T, bool> isVariadic)
    {
        foreach (var item in items)
        {
            if (string.Equals(getName(item), variableName, System.StringComparison.Ordinal))
            {
                return isVariadic(item);
            }
        }

        return false;
    }

    private static void AppendTypeField(
        HashSet<string> usedNames,
        string baseName,
        string operandName,
        OperationModel operation,
        OperationBodySyntaxMetadata metadata,
        bool nullable,
        BodyComponentKind componentKind)
    {
        var name = EmitterHelpers.MakeUnique(baseName, usedNames);
        var isVariadic = IsVariadicOperand(operation, operandName) || IsVariadicResult(operation, operandName);
        var (csType, writeStmt) = GetTypeFieldShape(name, nullable, isVariadic);
        AddBodySyntaxField(metadata, componentKind, operandName, name, csType, writeStmt);
    }

    private static (string CsType, string WriteStmt) GetTypeFieldShape(string name, bool nullable, bool isVariadic)
    {
        if (isVariadic)
        {
            return (
                "IReadOnlyList<TypeSyntax>",
                "for (var i = 0; i < " + name + ".Count; i++)\n" +
                "{\n" +
                "    if (i > 0)\n" +
                "    {\n" +
                "        writer.WriteToken(TokenFactory.Comma());\n" +
                "    }\n" +
                "\n" +
                "    writer.SuggestTrivia(\" \");\n" +
                "    " + name + "[i].WriteTo(writer);\n" +
                "}");
        }

        return GetSyntaxNodeFieldShape(name, "TypeSyntax", nullable);
    }

    private static string GetTypeBaseName(DirectiveOperand operand)
    {
        return operand is VariableOperand varOp
            ? DialectGeneratorNaming.ToPascalCase(varOp.Name) + "Type"
            : "Type";
    }

    private static string GetQualifiedTypeBaseName(DirectiveOperand operand)
    {
        if (operand is TypeDirectiveOperand tdo && tdo.Operand is VariableOperand tVar)
        {
            return DialectGeneratorNaming.ToPascalCase(tVar.Name) + "Type";
        }

        return GetTypeBaseName(operand);
    }

    private static string GetPunctuationLeadingTrivia(TokenKind tokenKind)
    {
        return tokenKind switch
        {
            TokenKind.Colon => " ",
            TokenKind.Arrow => " ",
            TokenKind.Equal => " ",
            _ => string.Empty,
        };
    }

    private static string GetPunctuationFieldName(TokenKind tokenKind)
    {
        return tokenKind switch
        {
            TokenKind.Comma => "CommaToken",
            TokenKind.LParen => "LParenToken",
            TokenKind.RParen => "RParenToken",
            TokenKind.LBracket => "LBracketToken",
            TokenKind.RBracket => "RBracketToken",
            TokenKind.LBrace => "LBraceToken",
            TokenKind.RBrace => "RBraceToken",
            TokenKind.Arrow => "ArrowToken",
            TokenKind.Colon => "ColonToken",
            TokenKind.Equal => "EqualToken",
            TokenKind.LessThan => "LessThanToken",
            TokenKind.GreaterThan => "GreaterThanToken",
            TokenKind.Question => "QuestionToken",
            TokenKind.Star => "StarToken",
            TokenKind.Plus => "PlusToken",
            TokenKind.Minus => "MinusToken",
            TokenKind.Dot => "DotToken",
            TokenKind.At => "AtToken",
            TokenKind.Hash => "HashToken",
            TokenKind.SymbolName => "SymbolNameToken",
            _ => "Token",
        };
    }

    private static AttrOrTypeParameterModel? FindParameter(
        IReadOnlyList<AttrOrTypeParameterModel> parameters,
        string variableName)
    {
        foreach (var param in parameters)
        {
            if (string.Equals(param.Name, variableName, System.StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }

    public static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
    {
        if (param == null)
        {
            return "AttributeValueSyntax";
        }

        if (!string.IsNullOrEmpty(param.CsharpType))
        {
            return param.CsharpType!;
        }

        return "AttributeValueSyntax";
    }

    private static string GetResolvedCSharpSyntaxType(AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpSyntaxType))
        {
            return param!.CsharpSyntaxType!;
        }

        return "AttributeValueSyntax";
    }

    private static SyntaxValueShape GetResolvedCSharpSyntaxShape(AttrOrTypeParameterModel? param)
    {
        return param?.CsharpSyntaxShape ?? SyntaxValueShape.SyntaxNode;
    }
}

internal sealed class LoweredAssemblyFormat
{
    public LoweredAssemblyFormat(IReadOnlyList<FormatSlot> slots)
    {
        Slots = slots;
    }

    public IReadOnlyList<FormatSlot> Slots { get; }
}

internal sealed class LoweredOperationAssemblyFormat
{
    public LoweredOperationAssemblyFormat(
        IReadOnlyList<LoweredOperationElement> elements,
        OperationBodySyntaxMetadata metadata)
    {
        Elements = elements;
        Metadata = metadata;
    }

    public IReadOnlyList<LoweredOperationElement> Elements { get; }

    public OperationBodySyntaxMetadata Metadata { get; }

    public bool IsSupported
    {
        get
        {
            foreach (var element in Elements)
            {
                if (!element.IsSupported)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

internal enum OperationFormatElementKind
{
    Unsupported,
    Literal,
    Variable,
    AttrDict,
    AttrDictWithKeyword,
    PropDict,
    Type,
    QualifiedType,
    ResultsType,
    FunctionalType,
    Regions,
    Successors,
    Operands,
    OptionalGroup,
    Oilist,
}

internal sealed class LoweredOperationElement
{
    public LoweredOperationElement(
        Element source,
        OperationFormatElementKind kind,
        int siblingIndex,
        int fieldStart,
        int fieldCount,
        bool isSupported)
    {
        Source = source;
        Kind = kind;
        SiblingIndex = siblingIndex;
        FieldStart = fieldStart;
        FieldCount = fieldCount;
        IsSupported = isSupported;
    }

    public Element Source { get; }

    public OperationFormatElementKind Kind { get; }

    public int SiblingIndex { get; }

    public int FieldStart { get; }

    public int FieldCount { get; }

    public bool IsSupported { get; }
}

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
