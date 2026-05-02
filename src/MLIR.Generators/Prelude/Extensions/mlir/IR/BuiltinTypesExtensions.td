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

extends Builtin_Integer : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    Builtin_IntegerWidthParameter:$width,
    Builtin_IntegerSignednessParameter:$signedness
  );
  let csharpName = "Signedness switch { global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signed => \"si\" + Width, global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Unsigned => \"ui\" + Width, _ => \"i\" + Width }";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinIntegerTypeAssemblyFormat()";
}

// Assembly formats and C# names for scalar float types.
// Each type supplies a constructor lambda so the shared BuiltinScalarFloatTypeAssemblyFormat
// can produce the correct concrete TypeReference subclass.
// csharpName overrides the Name property to return the short MLIR mnemonic (e.g., "f32")
// rather than the qualified registry key (e.g., "builtin.f32").

extends Builtin_Float8E5M2 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E5M2\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E5M2Type(syntax))";
}

extends Builtin_Float8E4M3 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3Type(syntax))";
}

extends Builtin_Float8E4M3FN : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3FNType(syntax))";
}

extends Builtin_Float8E5M2FNUZ : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E5M2FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E5M2FNUZType(syntax))";
}

extends Builtin_Float8E4M3FNUZ : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3FNUZType(syntax))";
}

extends Builtin_Float8E4M3B11FNUZ : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3B11FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3B11FNUZType(syntax))";
}

extends Builtin_Float8E3M4 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E3M4\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E3M4Type(syntax))";
}

extends Builtin_Float4E2M1FN : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f4E2M1FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float4E2M1FNType(syntax))";
}

extends Builtin_Float6E2M3FN : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f6E2M3FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float6E2M3FNType(syntax))";
}

extends Builtin_Float6E3M2FN : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f6E3M2FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float6E3M2FNType(syntax))";
}

extends Builtin_Float8E8M0FNU : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f8E8M0FNU\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E8M0FNUType(syntax))";
}

extends Builtin_BFloat16 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"bf16\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.BFloat16Type(syntax))";
}

extends Builtin_Float16 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f16\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float16Type(syntax))";
}

extends Builtin_FloatTF32 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"tf32\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.FloatTF32Type(syntax))";
}

extends Builtin_Float32 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f32\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float32Type(syntax))";
}

extends Builtin_Float64 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f64\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float64Type(syntax))";
}

extends Builtin_Float80 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f80\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float80Type(syntax))";
}

extends Builtin_Float128 : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"f128\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float128Type(syntax))";
}

// Assembly formats and C# names for scalar non-float types (index, none).

extends Builtin_Index : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"index\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinIndexTypeAssemblyFormat()";
}

extends Builtin_None : CSharpAttrOrTypeDefExtension {
  let csharpName = "\"none\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinNoneTypeAssemblyFormat()";
}

// Aggregate builtin types are described upstream as TypeDef records. MLIR.NET keeps their
// parser/printer logic in hand-written assembly-format helpers because their textual forms are
// core MLIR syntax rather than dialect-prefixed declarative formats, but the generated TypeDef
// classes own the canonical TypeDefinition and semantic payload.

extends Builtin_Function : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$inputs,
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$results
  );
  let csharpName = "\"function\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinFunctionTypeAssemblyFormat()";
}

extends Builtin_Tuple : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>":$types
  );
  let csharpName = "\"tuple\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinTupleTypeAssemblyFormat()";
}

extends Builtin_RankedTensor : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpName = "\"tensor\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinTensorTypeAssemblyFormat()";
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_UnrankedTensor : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpName = "\"tensor\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinTensorTypeAssemblyFormat()";
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "false">,
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "Shape", "throw new global::System.InvalidOperationException(\"Unranked shaped types do not have a shape.\")">
  ];
}

extends Builtin_Vector : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpName = "\"vector\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinVectorTypeAssemblyFormat()";
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_MemRef : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::System.Collections.Generic.IReadOnlyList<long>":$shape,
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpName = "\"memref\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinMemRefTypeAssemblyFormat()";
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "true">
  ];
}

extends Builtin_UnrankedMemRef : CSharpAttrOrTypeDefExtension {
  let csharpParameters = (ins
    "global::MLIR.Semantics.TypeReference":$elementType
  );
  let csharpName = "\"memref\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinMemRefTypeAssemblyFormat()";
  let csharpInterfaceImplementations = [
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "HasRank", "false">,
    CSharpInterfacePropertyImplementation<ShapedTypeInterface, "Shape", "throw new global::System.InvalidOperationException(\"Unranked shaped types do not have a shape.\")">
  ];
}

#endif // BUILTIN_TYPES_EXTENSIONS_TD
