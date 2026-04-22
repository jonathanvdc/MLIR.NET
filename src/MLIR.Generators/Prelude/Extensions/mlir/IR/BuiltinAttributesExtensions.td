include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef BUILTIN_ATTRIBUTES_EXTENSIONS_TD
#define BUILTIN_ATTRIBUTES_EXTENSIONS_TD

// MLIR.NET-specific parameter overlays for builtin attribute defs.

def Builtin_TypeParameter : AttrOrTypeParameter<"Type", "nested type">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::MLIR.Semantics.TypeReference";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax";
  let csharpExtractor = "$_syntax.TypeSyntax";
}

def Builtin_ArrayParameter : AttrOrTypeParameter<
    "ArrayRef<Attribute>", "array of attributes">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.Collections.ArrayAttributeValueSyntax";
  let csharpExtractor = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.BindItemsFromSyntax($_syntax.Items.Items)";
  let csharpDefault = "global::System.Array.Empty<global::MLIR.Semantics.AttributeValue>()";
}

def Builtin_DictionaryParameter : AttrOrTypeParameter<
    "ArrayRef<NamedAttribute>", "named attributes">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax";
  let csharpExtractor = "global::MLIR.Dialects.Attributes.Collections.DictionaryAttributeAssemblyFormat.BindAttributesFromSyntax($_syntax.Attributes.Items)";
  let csharpDefault = "global::MLIR.Semantics.NamedAttributeCollection.Empty";
}

def Builtin_StringAttrParameter : AttrOrTypeParameter<"StringAttr", "string attribute">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "string";
  let csharpSyntaxType = "StringAttributeValueSyntax";
  let csharpParser = "$_parser.TryParseStringLiteralSyntax()";
  let csharpExtractor = "$_syntax.Value";
  let csharpDefault = "string.Empty";
  let csharpPrinter = "new StringAttributeValueSyntax(TokenFactory.StringLiteral(StringLiteralAttributeAssemblyFormat.Quote($_self)), $_self)";
}

extends Builtin_DenseArrayRawDataParameter : MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::System.ReadOnlyMemory<byte>";
  let csharpDefault = "global::System.ReadOnlyMemory<byte>.Empty";
}

extends Builtin_TypeAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_TypeParameter:$value);
}

extends Builtin_UnitAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Primitives.UnitLiteralAttributeAssemblyFormat()";
}

extends Builtin_DenseArray : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    Builtin_TypeParameter:$elementType,
    "long":$size,
    Builtin_DenseArrayRawDataParameter:$rawData
  );
}

extends Builtin_DenseTypedElementsAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::MLIR.Semantics.AttributeValue":$rawData
  );
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Collections.ElementsAttributeAssemblyFormat()";
}

extends Builtin_ArrayAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_ArrayParameter:$value);
}

extends Builtin_DictionaryAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_DictionaryParameter:$value);
}

extends Builtin_DenseStringElementsAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::System.Collections.Generic.IReadOnlyList<string>":$value
  );
}

extends Builtin_DenseResourceElementsAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "string":$rawHandle
  );
}

extends Builtin_OpaqueAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_StringAttrParameter:$dialectNamespace);
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinOpaqueAttributeAssemblyFormat()";
}

extends Builtin_SparseElementsAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    AttributeSelfTypeParameter<"", "ShapedType">:$type,
    "global::MLIR.Semantics.AttributeValue":$indices,
    "global::MLIR.Semantics.AttributeValue":$values
  );
}

extends Builtin_SymbolRefAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    "string":$rootReference,
    "global::System.Collections.Generic.IReadOnlyList<string>":$nestedReferences
  );
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinSymbolRefAttributeAssemblyFormat()";
}

extends StridedLayoutAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins
    "long?":$offset,
    "global::System.Collections.Generic.IReadOnlyList<long?>":$strides
  );
}

#endif // BUILTIN_ATTRIBUTES_EXTENSIONS_TD
