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
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.String($0)";
}

extends BoolAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.BooleanAttributeValue";
  let csharpReturnType = "bool";
  let csharpConvertFromStorage = "$_self.Value";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Bool($0)";
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

extends ArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.ArrayAttributeValue";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends TypedArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<global::MLIR.Semantics.AttributeValue>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>";
  let csharpConvertFromStorage = "$_self.Items";
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

extends FloatAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.FloatingPointAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.FloatingPointAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F32Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.FloatingPointAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends F64Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.FloatingPointAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends BF16Attr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Primitives.FloatingPointAttributeValue";
  let csharpReturnType = "global::MLIR.Numerics.ApFloat";
  let csharpConvertFromStorage = "$_self.Value";
}

extends DenseArrayAttrBase : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlyMemory<byte>";
  let csharpConvertFromStorage = "$_self.RawData";
}

extends DenseBoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<bool>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseBool($0)";
}

extends DenseI8ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<sbyte>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI8($0)";
}

extends DenseI16ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<short>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI16($0)";
}

extends DenseI32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<int>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI32($0)";
}

extends DenseI64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<long>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseI64($0)";
}

extends DenseF32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<float>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF32($0)";
}

extends DenseF64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Builtin.DenseArrayAttr";
  let csharpReturnType = "global::System.ReadOnlySpan<double>";
  let csharpConvertFromStorage = "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, double>($_self.RawData.Span)";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.DenseF64($0)";
}

extends BoolArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<bool>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<bool>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends I32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<int>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<int>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends I64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<long>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends I64SmallVectorArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<long>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<long>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends F32ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<float>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<float>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends F64ArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<double>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<double>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends StrArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<string>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<string>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends TypeArrayAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<global::MLIR.Syntax.TypeSyntax>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Syntax.TypeSyntax>";
  let csharpConvertFromStorage = "$_self.Items";
}

extends SymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.SymbolRefAttr";
  let csharpReturnType = "global::MLIR.Semantics.SymbolRefAttr";
  let csharpConvertFromStorage = "$_self";
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.SymbolRef($0)";
}

extends FlatSymbolRefAttr : MLIRNet_AttrExtension {
  let csharpStorageType = "global::MLIR.Semantics.SymbolRefAttr";
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
  let csharpStorageType = "global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<global::MLIR.Semantics.NamedAttributeCollection>";
  let csharpReturnType = "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection>";
  let csharpConvertFromStorage = "$_self.Items";
}

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
