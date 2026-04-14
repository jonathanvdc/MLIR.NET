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
  let csharpStorageType = "global::MLIR.StringAttr";
  let csharpReturnType = "string";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.String($0)";
}

extends BoolAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Bool($0)";
}

extends TypeAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.TypeAttr";
  let csharpReturnType = "global::MLIR.Semantics.TypeReference";
  let csharpConvertFromStorage = "$_self.Value";
}

extends UnitAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.UnitAttr";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self != null";
  let csharpDefaultValue = "false";
}

extends DictionaryAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DictionaryAttr";
  let csharpReturnType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpConvertFromStorage = "$_self.Value";
}

extends ElementsAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.ElementsAttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpConvertFromStorage = "$_self.Payload";
}

extends ArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Array($0)";
}

// Integer attrs all use the semantic integer-value storage layer; the concrete leaf
// definitions refine only the exposed return type and conversion expression.
extends AnyIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignlessIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends UnsignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpConvertFromStorage = "$_self.Value";
}

extends I1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends I8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "byte";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends I16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "ushort";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends I32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "uint";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.I32($0)";
}

extends I64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "ulong";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

extends SI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToInt64() != 0";
}

extends SI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "sbyte";
  let csharpConvertFromStorage = "(sbyte)$_self.Value.ToInt64()";
}

extends SI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "short";
  let csharpConvertFromStorage = "(short)$_self.Value.ToInt64()";
}

extends SI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "int";
  let csharpConvertFromStorage = "(int)$_self.Value.ToInt64()";
}

extends SI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "long";
  let csharpConvertFromStorage = "$_self.Value.ToInt64()";
}

extends UI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends UI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "byte";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends UI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "ushort";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends UI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "uint";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
}

extends UI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.IntegerAttr";
  let csharpReturnType = "ulong";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

extends FloatAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F32($0)";
}

extends F64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F64($0)";
}

extends BF16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends DenseArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlyMemory<byte>";
  let csharpConvertFromStorage = "$_self.RawData";
}

extends DenseBoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<bool>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseBool($0)";
}

extends DenseI8ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<sbyte>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI8($0)";
}

extends DenseI16ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<short>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI16($0)";
}

extends DenseI32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<int>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI32($0)";
}

extends DenseI64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<long>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI64($0)";
}

extends DenseF32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<float>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF32($0)";
}

extends DenseF64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<double>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, double>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF64($0)";
}

extends BoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<bool>";
  let csharpConvertFromStorage = "global::MLIR.BoolArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.BoolArrayAttrConstraintAttributeValue.Create($0)";
}

extends I32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<int>";
  let csharpConvertFromStorage = "global::MLIR.I32ArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.I32ArrayAttrConstraintAttributeValue.Create($0)";
}

extends I64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpConvertFromStorage = "global::MLIR.I64ArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.I64ArrayAttrConstraintAttributeValue.Create($0)";
}

extends I64SmallVectorArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpConvertFromStorage = "global::MLIR.I64SmallVectorArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.I64SmallVectorArrayAttrConstraintAttributeValue.Create($0)";
}

extends F32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<float>";
  let csharpConvertFromStorage = "global::MLIR.F32ArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.F32ArrayAttrConstraintAttributeValue.Create($0)";
}

extends F64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<double>";
  let csharpConvertFromStorage = "global::MLIR.F64ArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.F64ArrayAttrConstraintAttributeValue.Create($0)";
}

extends StrArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<string>";
  let csharpConvertFromStorage = "global::MLIR.StrArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.StrArrayAttrConstraintAttributeValue.Create($0)";
}

extends TypeArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>";
  let csharpConvertFromStorage = "global::MLIR.TypeArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.TypeArrayAttrConstraintAttributeValue.Create($0)";
}

extends SymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.SymbolRefAttr";
  let csharpReturnType = "global::MLIR.SymbolRefAttr";
  let csharpConvertFromStorage = "$_self";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.SymbolRef($0)";
}

extends FlatSymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.SymbolRefAttr";
  let csharpReturnType = "string";
  let csharpConvertFromStorage = "$_self.RootReference";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.FlatSymbolRef($0)";
}

extends AffineMapAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpConvertFromStorage = "$_self";
}

extends IntegerSetAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpConvertFromStorage = "$_self";
}

extends DictArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection>";
  let csharpConvertFromStorage = "global::MLIR.DictArrayAttrConstraintAttributeValue.GetItems($_self)";
  let csharpConstBuilderCall = "global::MLIR.DictArrayAttrConstraintAttributeValue.Create($0)";
}

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
