include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
#define COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD

// Preserve a generic C# view for all Attr-style constraints unless a more specific
// overlay below refines the storage/result relationship.
extends Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpConvertFromStorage = "$_self";
}

extends StringBasedAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.StringAttributeValue";
  let csharpReturnType = "string";
  let csharpConvertFromStorage = "$_self.Value";
}

extends BoolAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.BooleanAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value";
}

extends TypeAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.TypeAttributeValue";
  let csharpReturnType = "global::MLIR.Syntax.TypeSyntax";
  let csharpConvertFromStorage = "$_self.TypeSyntax";
}

extends UnitAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.UnitAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self != null";
  let csharpDefaultValue = "false";
}

extends DictionaryAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.DictionaryAttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpConvertFromStorage = "$_self.Attributes";
}

extends ElementsAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.ElementsAttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpConvertFromStorage = "$_self.Payload";
}

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
