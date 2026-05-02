include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
#define COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD

// Preserve a generic C# view for all Attr-style constraints unless a more specific
// overlay below refines the storage/result relationship.
extends Attr : MLIRNet_IdentityAttrExtension<"global::MLIR.Semantics.AttributeValue">;

extends StringBasedAttr : MLIRNet_ReferenceAttrValueExtension<
    "global::MLIR.Dialects.Builtin.StringAttr",
    "string",
    "$_self.Value"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.String($0)";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Primitives.StringLiteralAttributeAssemblyFormat()";
}

extends BoolAttr : MLIRNet_ValueAttrValueExtension<
    "global::MLIR.Dialects.Builtin.IntegerAttr",
    "bool",
    "$_self.Value.ToUInt64() != 0"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Bool($0)";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Primitives.BooleanLiteralAttributeAssemblyFormat()";
}

extends TypeAttr : MLIRNet_IdentityAttrExtension<"global::MLIR.Dialects.Builtin.TypeAttr"> {
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.TypeAttributeAssemblyFormat()";
}

extends UnitAttr : MLIRNet_IdentityAttrExtension<"global::MLIR.Dialects.Builtin.UnitAttr"> {
  let csharpOptionalAttributeRepresentation = "PresenceBoolean";
  let csharpPresenceAttributeValue = "global::MLIR.Semantics.ConstantAttributeFactory.Unit";
  let csharpPresenceSyntax = "new UnitAttributeValueSyntax(${token})";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Primitives.UnitLiteralAttributeAssemblyFormat()";
}

extends DictionaryAttrBase : MLIRNet_IdentityAttrExtension<"global::MLIR.Dialects.Builtin.DictionaryAttr"> {
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Collections.DictionaryAttributeAssemblyFormat()";
}

extends ElementsAttrBase : MLIRNet_IdentityAttrExtension<"global::MLIR.Dialects.Builtin.DenseTypedElementsAttr"> {
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Attributes.Collections.ElementsAttributeAssemblyFormat()";
}

extends ArrayAttrBase : MLIRNet_ReferenceAttrValueExtension<
    "global::MLIR.Dialects.Builtin.ArrayAttr",
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.AttributeValue>",
    "$_self.Value"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.Array($0)";
}

// Integer attrs all use the semantic integer-value storage layer; the concrete leaf
// definitions refine only the exposed return type and conversion expression.
extends AnyIntegerAttrBase : MLIRNet_IntegerAttrExtension<"global::MLIR.Numerics.ApInt", "$_self.Value">;

extends SignlessIntegerAttrBase : MLIRNet_IntegerAttrExtension<"global::MLIR.Numerics.ApInt", "$_self.Value">;

extends SignedIntegerAttrBase : MLIRNet_IntegerAttrExtension<"global::MLIR.Numerics.ApInt", "$_self.Value">;

extends UnsignedIntegerAttrBase : MLIRNet_IntegerAttrExtension<"global::MLIR.Numerics.ApInt", "$_self.Value">;

extends I1Attr : MLIRNet_IntegerAttrExtension<"bool", "$_self.Value.ToUInt64() != 0">;

extends I8Attr : MLIRNet_IntegerAttrExtension<"byte", "(byte)$_self.Value.ToUInt64()">;

extends I16Attr : MLIRNet_IntegerAttrExtension<"ushort", "(ushort)$_self.Value.ToUInt64()">;

extends I32Attr : MLIRNet_IntegerAttrExtension<"uint", "(uint)$_self.Value.ToUInt64()"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.I32($0)";
}

extends I64Attr : MLIRNet_IntegerAttrExtension<"ulong", "$_self.Value.ToUInt64()">;

extends SI1Attr : MLIRNet_IntegerAttrExtension<"bool", "$_self.Value.ToInt64() != 0">;

extends SI8Attr : MLIRNet_IntegerAttrExtension<"sbyte", "(sbyte)$_self.Value.ToInt64()">;

extends SI16Attr : MLIRNet_IntegerAttrExtension<"short", "(short)$_self.Value.ToInt64()">;

extends SI32Attr : MLIRNet_IntegerAttrExtension<"int", "(int)$_self.Value.ToInt64()">;

extends SI64Attr : MLIRNet_IntegerAttrExtension<"long", "$_self.Value.ToInt64()">;

extends UI1Attr : MLIRNet_IntegerAttrExtension<"bool", "$_self.Value.ToUInt64() != 0">;

extends UI8Attr : MLIRNet_IntegerAttrExtension<"byte", "(byte)$_self.Value.ToUInt64()">;

extends UI16Attr : MLIRNet_IntegerAttrExtension<"ushort", "(ushort)$_self.Value.ToUInt64()">;

extends UI32Attr : MLIRNet_IntegerAttrExtension<"uint", "(uint)$_self.Value.ToUInt64()">;

extends UI64Attr : MLIRNet_IntegerAttrExtension<"ulong", "$_self.Value.ToUInt64()">;

extends FloatAttrBase : MLIRNet_FloatAttrExtension<
    "new global::MLIR.Dialects.Attributes.Primitives.FloatingPointLiteralAttributeAssemblyFormat()">;

extends F16Attr : MLIRNet_FloatAttrExtension<
    "new global::MLIR.Dialects.Attributes.Primitives.FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEHalf)">;

extends F32Attr : MLIRNet_FloatAttrExtension<
    "new global::MLIR.Dialects.Attributes.Primitives.FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEESingle)"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F32($0)";
}

extends F64Attr : MLIRNet_FloatAttrExtension<
    "new global::MLIR.Dialects.Attributes.Primitives.FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEDouble)"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.F64($0)";
}

extends BF16Attr : MLIRNet_FloatAttrExtension<
    "new global::MLIR.Dialects.Attributes.Primitives.FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.BFloat16)">;

extends DenseArrayAttrBase : MLIRNet_ReferenceAttrValueExtension<
    "global::MLIR.Dialects.Builtin.DenseArrayAttr",
    "global::System.ReadOnlyMemory<byte>",
    "$_self.RawData">;

extends DenseBoolArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<bool>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseBool($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseBooleanArrayAttributeAssemblyFormat()">;

extends DenseI8ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<sbyte>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, sbyte>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseI8($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseIntegerArrayAttributeAssemblyFormat()">;

extends DenseI16ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<short>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseI16($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseIntegerArrayAttributeAssemblyFormat()">;

extends DenseI32ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<int>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseI32($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseIntegerArrayAttributeAssemblyFormat()">;

extends DenseI64ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<long>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, long>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseI64($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseIntegerArrayAttributeAssemblyFormat()">;

extends DenseF32ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<float>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseF32($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseFloatingPointArrayAttributeAssemblyFormat(\"f32\")">;

extends DenseF64ArrayAttr : MLIRNet_DenseArrayAttrExtension<
    "global::System.ReadOnlySpan<double>",
    "global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, double>($_self.RawData.Span)",
    "global::MLIR.Semantics.ConstantAttributeFactory.DenseF64($0)",
    "new global::MLIR.Dialects.Attributes.Collections.DenseFloatingPointArrayAttributeAssemblyFormat(\"f64\")">;

extends BoolArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<bool>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToUInt64() != 0)",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.Bool(element))">;

extends I32ArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<int>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => (int)((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.I32(unchecked((uint)element)))">;

extends I64ArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<long>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I64, global::MLIR.Numerics.ApInt.FromInt64(64, element), null))">;

extends I64SmallVectorArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<long>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.IntegerAttr)itemValue).Value.ToInt64())",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I64, global::MLIR.Numerics.ApInt.FromInt64(64, element), null))">;

extends F32ArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<float>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.FloatAttr)itemValue).Value.ToSingle())",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F32, global::MLIR.Numerics.ApFloat.FromDouble(global::MLIR.Numerics.FloatSemantics.IEEESingle, element), null))">;

extends F64ArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<double>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.FloatAttr)itemValue).Value.ToDouble())",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, global::MLIR.Numerics.ApFloat.FromDouble(global::MLIR.Numerics.FloatSemantics.IEEEDouble, element), null))">;

extends StrArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<string>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.StringAttr)itemValue).Value)",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => global::MLIR.Semantics.ConstantAttributeFactory.String(element))">;

extends TypeArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.TypeAttr)itemValue).Value)",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.TypeAttr(element, null))">;

extends SymbolRefAttr : MLIRNet_IdentityAttrExtension<"global::MLIR.Dialects.Builtin.SymbolRefAttr"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.SymbolRef($0)";
}

extends FlatSymbolRefAttr : MLIRNet_ReferenceAttrValueExtension<
    "global::MLIR.Dialects.Builtin.SymbolRefAttr",
    "string",
    "$_self.RootReference"> {
  let csharpConstBuilderCall = "global::MLIR.Semantics.ConstantAttributeFactory.FlatSymbolRef($0)";
}

extends AffineMapAttr : MLIRNet_IdentityAttrExtension<"global::MLIR.Semantics.AttributeValue">;

extends IntegerSetAttr : MLIRNet_IdentityAttrExtension<"global::MLIR.Semantics.AttributeValue">;

extends DictArrayAttr : MLIRNet_ArrayAttrExtension<
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection>",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.GetItems($_self, static itemValue => ((global::MLIR.Dialects.Builtin.DictionaryAttr)itemValue).Value)",
    "global::MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.Create($0, static element => new global::MLIR.Dialects.Builtin.DictionaryAttr(element, null))">;

#endif // COMMON_ATTR_CONSTRAINTS_EXTENSIONS_TD
