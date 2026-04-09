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
  let csharpExtractor = "global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeItems($_syntax.Items.Items)";
  let csharpDefault = "global::System.Array.Empty<global::MLIR.Semantics.AttributeValue>()";
}

def Builtin_DictionaryParameter : AttrOrTypeParameter<
    "ArrayRef<NamedAttribute>", "named attributes">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpSyntaxType = "global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax";
  let csharpExtractor = "global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes($_syntax.Attributes.Items)";
  let csharpDefault = "global::MLIR.Semantics.NamedAttributeCollection.Empty";
}

extends Builtin_TypeAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_TypeParameter:$value);
}

extends Builtin_ArrayAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_ArrayParameter:$value);
}

extends Builtin_DictionaryAttr : MLIRNet_AttrOrTypeDefExtension {
  let csharpParameters = (ins Builtin_DictionaryParameter:$value);
}

#endif // BUILTIN_ATTRIBUTES_EXTENSIONS_TD
