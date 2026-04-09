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

// Integer attrs all use the semantic integer-value storage layer; the concrete leaf
// definitions refine only the exposed return type and conversion expression.
extends AnyIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignlessIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends UnsignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends I1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends I8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "byte";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends I16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "ushort";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends I32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "uint";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
}

extends I64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "ulong";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

extends SI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToInt64() != 0";
}

extends SI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "sbyte";
  let csharpConvertFromStorage = "(sbyte)$_self.Value.ToInt64()";
}

extends SI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "short";
  let csharpConvertFromStorage = "(short)$_self.Value.ToInt64()";
}

extends SI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "int";
  let csharpConvertFromStorage = "(int)$_self.Value.ToInt64()";
}

extends SI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "long";
  let csharpConvertFromStorage = "$_self.Value.ToInt64()";
}

extends UI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends UI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "byte";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends UI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "ushort";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends UI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "uint";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
}

extends UI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.IntegerAttributeValue";
  let csharpReturnType = "ulong";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
