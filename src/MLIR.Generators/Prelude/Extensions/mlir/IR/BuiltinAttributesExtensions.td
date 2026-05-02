include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef BUILTIN_ATTRIBUTES_EXTENSIONS_TD
#define BUILTIN_ATTRIBUTES_EXTENSIONS_TD

// MLIR.NET-specific parameter overlays for builtin attribute defs.

def Builtin_TypeParameter : AttrOrTypeParameter<"Type", "nested type">,
    CSharpParameterExtension {
  let csharpType = "global::MLIR.Semantics.TypeReference";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax";
  let csharpSyntaxShape = "SyntaxNode";
  let csharpExtractor = "$_syntax.TypeSyntax";
}

def Builtin_ArrayParameter : AttrOrTypeParameter<
    "ArrayRef<Attribute>", "array of attributes">,
    CSharpParameterExtension {
  let csharpType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.Collections.ArrayAttributeValueSyntax";
  let csharpSyntaxShape = "SyntaxNode";
  let csharpExtractor = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.BindItemsFromSyntax($_syntax.Items.Items)";
  let csharpDefault = "global::System.Array.Empty<global::MLIR.Semantics.AttributeValue>()";
}

def Builtin_DictionaryParameter : AttrOrTypeParameter<
    "ArrayRef<NamedAttribute>", "named attributes">,
    CSharpParameterExtension {
  let csharpType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax";
  let csharpSyntaxShape = "SyntaxNode";
  let csharpExtractor = "global::MLIR.Dialects.Attributes.Collections.DictionaryAttributeAssemblyFormat.BindAttributesFromSyntax($_syntax.Attributes.Items)";
  let csharpDefault = "global::MLIR.Semantics.NamedAttributeCollection.Empty";
}

def Builtin_StringAttrParameter : AttrOrTypeParameter<"StringAttr", "string attribute">,
    CSharpParameterExtension {
  let csharpType = "string";
  let csharpSyntaxType = "StringAttributeValueSyntax";
  let csharpSyntaxShape = "SyntaxNode";
  let csharpParser = "$_parser.TryParseStringLiteralSyntax()";
  let csharpExtractor = "$_syntax.Value";
  let csharpDefault = "string.Empty";
  let csharpPrinter = "new StringAttributeValueSyntax(TokenFactory.StringLiteral(StringLiteralAttributeAssemblyFormat.Quote($_self)), $_self)";
}

extends Builtin_DenseArrayRawDataParameter : CSharpParameterExtension {
  let csharpType = "global::System.ReadOnlyMemory<byte>";
  let csharpDefault = "global::System.ReadOnlyMemory<byte>.Empty";
}

extends Builtin_TypeAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_TypeParameter:$value);
}

extends Builtin_UnitAttr : CSharpAssemblyFormattedDef<
    "new global::MLIR.Dialects.Attributes.Primitives.UnitLiteralAttributeAssemblyFormat()">;

extends Builtin_DenseArray : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    Builtin_TypeParameter:$elementType,
    "long":$size,
    Builtin_DenseArrayRawDataParameter:$rawData
  );
}

extends Builtin_DenseTypedElementsAttr : CSharpAssemblyFormattedDef<
    "new global::MLIR.Dialects.Attributes.Collections.ElementsAttributeAssemblyFormat()"> {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::MLIR.Semantics.AttributeValue":$rawData
  );
}

extends Builtin_ArrayAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_ArrayParameter:$value);
}

extends Builtin_DictionaryAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_DictionaryParameter:$value);
}

extends Builtin_DenseStringElementsAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::System.Collections.Generic.IReadOnlyList<string>":$value
  );
}

extends Builtin_DenseResourceElementsAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "string":$rawHandle
  );
}

extends Builtin_OpaqueAttr : CSharpAssemblyFormattedDef<
    "new global::MLIR.Dialects.Builtin.BuiltinOpaqueAttributeAssemblyFormat()"> {
  let csharpParameters = (ins Builtin_StringAttrParameter:$dialectNamespace);
}

extends Builtin_SparseElementsAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::MLIR.Semantics.AttributeValue":$indices,
    "global::MLIR.Semantics.AttributeValue":$values
  );
}

extends Builtin_SymbolRefAttr : CSharpAssemblyFormattedDef<
    "new global::MLIR.Dialects.Builtin.BuiltinSymbolRefAttributeAssemblyFormat()"> {
  let csharpParameters = (ins
    "string":$rootReference,
    "global::System.Collections.Generic.IReadOnlyList<string>":$nestedReferences
  );
}

extends StridedLayoutAttr : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "long?":$offset,
    "global::System.Collections.Generic.IReadOnlyList<long?>":$strides
  );
}

#endif // BUILTIN_ATTRIBUTES_EXTENSIONS_TD
