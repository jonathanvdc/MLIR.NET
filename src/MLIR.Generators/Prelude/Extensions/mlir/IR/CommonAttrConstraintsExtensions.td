include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
#define COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD

// Preserve a generic C# view for all Attr-style constraints unless a more specific
// overlay below refines the storage/result relationship.
extends Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self";
}

extends StringBasedAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.StringAttr";
  let csharpReturnType = "string";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.String($0)";
}

extends BoolAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Bool($0)";
}

extends TypeAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.TypeAttr";
  let csharpReturnType = "global::MLIR.Semantics.TypeReference";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.Value";
}

extends UnitAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.UnitAttr";
  let csharpReturnType = "bool";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self != null";
  let csharpDefaultValue = "false";
}

extends DictionaryAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DictionaryAttr";
  let csharpReturnType = "global::MLIR.Semantics.NamedAttributeCollection";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.Value";
}

extends ElementsAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseTypedElementsAttr";
  let csharpReturnType = "global::MLIR.Dialects.Builtin.DenseTypedElementsAttr";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self";
}

extends ArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Array($0)";
}

// Integer attrs all use the semantic integer-value storage layer; the concrete leaf
// definitions refine only the exposed return type and conversion expression.
extends AnyIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignlessIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends SignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends UnsignedIntegerAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApInt";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends I1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends I8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "byte";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends I16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "ushort";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends I32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "uint";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.I32($0)";
}

extends I64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "ulong";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

extends SI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToInt64() != 0";
}

extends SI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "sbyte";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(sbyte)$_self.Value.ToInt64()";
}

extends SI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "short";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(short)$_self.Value.ToInt64()";
}

extends SI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "int";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(int)$_self.Value.ToInt64()";
}

extends SI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "long";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToInt64()";
}

extends UI1Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "bool";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64() != 0";
}

extends UI8Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "byte";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(byte)$_self.Value.ToUInt64()";
}

extends UI16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "ushort";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(ushort)$_self.Value.ToUInt64()";
}

extends UI32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "uint";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "(uint)$_self.Value.ToUInt64()";
}

extends UI64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.IntegerAttr";
  let csharpReturnType = "ulong";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value.ToUInt64()";
}

extends FloatAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F32($0)";
}

extends F64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F64($0)";
}

extends BF16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.FloatAttr";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpOptionalValueAccess = "NullableValueType";
  let csharpConvertFromStorage = "$_self.Value";
}

extends DenseArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlyMemory<byte>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.RawData";
}

extends DenseBoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<bool>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseBool($0)";
}

extends DenseI8ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<sbyte>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI8($0)";
}

extends DenseI16ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<short>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI16($0)";
}

extends DenseI32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<int>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI32($0)";
}

extends DenseI64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<long>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI64($0)";
}

extends DenseF32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<float>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF32($0)";
}

extends DenseF64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<double>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, double>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF64($0)";
}

extends BoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<bool>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToUInt64() != 0)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.Bool(element))";
}

extends I32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<int>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => (int)((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.I32(unchecked((uint)element)))";
}

extends I64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I64, global::MLIR.Numerics.ApInt.FromInt64(64, element), null))";
}

extends I64SmallVectorArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I64, global::MLIR.Numerics.ApInt.FromInt64(64, element), null))";
}

extends F32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<float>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.FloatAttr)itemValue).Value.ToSingle())";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F32, global::MLIR.Numerics.ApFloat.FromDouble(global::MLIR.Numerics.FloatSemantics.IEEESingle, element), null))";
}

extends F64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<double>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.FloatAttr)itemValue).Value.ToDouble())";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, global::MLIR.Numerics.ApFloat.FromDouble(global::MLIR.Numerics.FloatSemantics.IEEEDouble, element), null))";
}

extends StrArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<string>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.StringAttr)itemValue).Value)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.String(element))";
}

extends TypeArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.TypeAttr)itemValue).Value)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.TypeAttr(element, null))";
}

extends SymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.SymbolRefAttr";
  let csharpReturnType = "global::MLIR.Dialects.Builtin.SymbolRefAttr";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.SymbolRef($0)";
}

extends FlatSymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.SymbolRefAttr";
  let csharpReturnType = "string";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self.RootReference";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.FlatSymbolRef($0)";
}

extends AffineMapAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self";
}

extends IntegerSetAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.AttributeValue";
  let csharpReturnType = "global::MLIR.Semantics.AttributeValue";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "$_self";
}

extends DictArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Dialects.Builtin.ArrayAttr";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection>";
  let csharpOptionalValueAccess = "NullCheck";
  let csharpConvertFromStorage = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.DictionaryAttr)itemValue).Value)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.DictionaryAttr(element, null))";
}

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
