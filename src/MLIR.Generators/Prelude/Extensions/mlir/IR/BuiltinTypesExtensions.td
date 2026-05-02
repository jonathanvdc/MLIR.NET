include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef BUILTIN_TYPES_EXTENSIONS_TD
#define BUILTIN_TYPES_EXTENSIONS_TD

// MLIR.NET-specific C# metadata for builtin type parameters.

def Builtin_IntegerWidthParameter : AttrOrTypeParameter<"unsigned", "integer width">,
    CSharpParameterExtension {
  let csharpType = "int";
  let csharpExtractor = "$_syntax.Width";
  let csharpDefault = "0";
}

def Builtin_IntegerSignednessParameter : AttrOrTypeParameter<"SignednessSemantics", "integer signedness">,
    CSharpParameterExtension {
  let csharpType = "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness";
  let csharpExtractor = "$_syntax.Signedness";
  let csharpDefault = "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signless";
}

extends Builtin_Integer : CSharpNamedAssemblyFormattedDef<
    "Signedness switch { global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signed => \"si\" + Width, global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Unsigned => \"ui\" + Width, _ => \"i\" + Width }",
    "new global::MLIR.Dialects.Builtin.BuiltinIntegerTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    Builtin_IntegerWidthParameter:$width,
    Builtin_IntegerSignednessParameter:$signedness
  );
}

// Assembly formats and C# names for scalar float types.
// Each type supplies a constructor lambda so the shared BuiltinScalarFloatTypeAssemblyFormat
// can produce the correct concrete TypeReference subclass.
// csharpName overrides the Name property to return the short MLIR mnemonic (e.g., "f32")
// rather than the qualified registry key (e.g., "builtin.f32").

extends Builtin_Float8E5M2 : CSharpBuiltinScalarFloatType<"\"f8E5M2\"", "global::MLIR.Dialects.Builtin.Float8E5M2Type">;
extends Builtin_Float8E4M3 : CSharpBuiltinScalarFloatType<"\"f8E4M3\"", "global::MLIR.Dialects.Builtin.Float8E4M3Type">;
extends Builtin_Float8E4M3FN : CSharpBuiltinScalarFloatType<"\"f8E4M3FN\"", "global::MLIR.Dialects.Builtin.Float8E4M3FNType">;
extends Builtin_Float8E5M2FNUZ : CSharpBuiltinScalarFloatType<"\"f8E5M2FNUZ\"", "global::MLIR.Dialects.Builtin.Float8E5M2FNUZType">;
extends Builtin_Float8E4M3FNUZ : CSharpBuiltinScalarFloatType<"\"f8E4M3FNUZ\"", "global::MLIR.Dialects.Builtin.Float8E4M3FNUZType">;
extends Builtin_Float8E4M3B11FNUZ : CSharpBuiltinScalarFloatType<"\"f8E4M3B11FNUZ\"", "global::MLIR.Dialects.Builtin.Float8E4M3B11FNUZType">;
extends Builtin_Float8E3M4 : CSharpBuiltinScalarFloatType<"\"f8E3M4\"", "global::MLIR.Dialects.Builtin.Float8E3M4Type">;
extends Builtin_Float4E2M1FN : CSharpBuiltinScalarFloatType<"\"f4E2M1FN\"", "global::MLIR.Dialects.Builtin.Float4E2M1FNType">;
extends Builtin_Float6E2M3FN : CSharpBuiltinScalarFloatType<"\"f6E2M3FN\"", "global::MLIR.Dialects.Builtin.Float6E2M3FNType">;
extends Builtin_Float6E3M2FN : CSharpBuiltinScalarFloatType<"\"f6E3M2FN\"", "global::MLIR.Dialects.Builtin.Float6E3M2FNType">;
extends Builtin_Float8E8M0FNU : CSharpBuiltinScalarFloatType<"\"f8E8M0FNU\"", "global::MLIR.Dialects.Builtin.Float8E8M0FNUType">;
extends Builtin_BFloat16 : CSharpBuiltinScalarFloatType<"\"bf16\"", "global::MLIR.Dialects.Builtin.BFloat16Type">;
extends Builtin_Float16 : CSharpBuiltinScalarFloatType<"\"f16\"", "global::MLIR.Dialects.Builtin.Float16Type">;
extends Builtin_FloatTF32 : CSharpBuiltinScalarFloatType<"\"tf32\"", "global::MLIR.Dialects.Builtin.FloatTF32Type">;
extends Builtin_Float32 : CSharpBuiltinScalarFloatType<"\"f32\"", "global::MLIR.Dialects.Builtin.Float32Type">;
extends Builtin_Float64 : CSharpBuiltinScalarFloatType<"\"f64\"", "global::MLIR.Dialects.Builtin.Float64Type">;
extends Builtin_Float80 : CSharpBuiltinScalarFloatType<"\"f80\"", "global::MLIR.Dialects.Builtin.Float80Type">;
extends Builtin_Float128 : CSharpBuiltinScalarFloatType<"\"f128\"", "global::MLIR.Dialects.Builtin.Float128Type">;

// Assembly formats and C# names for scalar non-float types (index, none).

extends Builtin_Index : CSharpNamedAssemblyFormattedDef<
    "\"index\"",
    "new global::MLIR.Dialects.Builtin.BuiltinIndexTypeAssemblyFormat()">;

extends Builtin_None : CSharpNamedAssemblyFormattedDef<
    "\"none\"",
    "new global::MLIR.Dialects.Builtin.BuiltinNoneTypeAssemblyFormat()">;

// Aggregate builtin types are described upstream as TypeDef records. MLIR.NET keeps their
// parser/printer logic in hand-written assembly-format helpers because their textual forms are
// core MLIR syntax rather than dialect-prefixed declarative formats, but the generated TypeDef
// classes own the canonical TypeDefinition and semantic payload.

extends Builtin_Function : CSharpNamedAssemblyFormattedDef<
    "\"function\"",
    "new global::MLIR.Dialects.Builtin.BuiltinFunctionTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$inputs,
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$results
  );
}

extends Builtin_Tuple : CSharpNamedAssemblyFormattedDef<
    "\"tuple\"",
    "new global::MLIR.Dialects.Builtin.BuiltinTupleTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$types
  );
}

extends Builtin_RankedTensor : CSharpNamedAssemblyFormattedDef<
    "\"tensor\"",
    "new global::MLIR.Dialects.Builtin.BuiltinTensorTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_UnrankedTensor : CSharpNamedAssemblyFormattedDef<
    "\"tensor\"",
    "new global::MLIR.Dialects.Builtin.BuiltinTensorTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "false">,
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "Shape", "throw new global::System.InvalidOperationException(\"Unranked shaped types do not have a shape.\")">
  ];
}

extends Builtin_Vector : CSharpNamedAssemblyFormattedDef<
    "\"vector\"",
    "new global::MLIR.Dialects.Builtin.BuiltinVectorTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_MemRef : CSharpNamedAssemblyFormattedDef<
    "\"memref\"",
    "new global::MLIR.Dialects.Builtin.BuiltinMemRefTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_UnrankedMemRef : CSharpNamedAssemblyFormattedDef<
    "\"memref\"",
    "new global::MLIR.Dialects.Builtin.BuiltinMemRefTypeAssemblyFormat()"> {
  let csharpParameters = (ins
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "false">,
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "Shape", "throw new global::System.InvalidOperationException(\"Unranked shaped types do not have a shape.\")">
  ];
}

#endif // BUILTIN_TYPES_EXTENSIONS_TD
