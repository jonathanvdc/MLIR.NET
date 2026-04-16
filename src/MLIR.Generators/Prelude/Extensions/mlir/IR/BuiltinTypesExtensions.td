include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef BUILTIN_TYPES_EXTENSIONS_TD
#define BUILTIN_TYPES_EXTENSIONS_TD

// MLIR.NET-specific C# metadata for builtin type parameters.

def Builtin_IntegerWidthParameter : AttrOrTypeParameter<"unsigned", "integer width">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "int";
  let csharpExtractor = "$_syntax.Width";
  let csharpDefault = "0";
}

def Builtin_IntegerSignednessParameter : AttrOrTypeParameter<"SignednessSemantics", "integer signedness">,
    MLIRNet_AttrOrTypeParameterExtension {
  let csharpType = "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness";
  let csharpExtractor = "$_syntax.Signedness";
  let csharpDefault = "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signless";
}

extends Builtin_Integer : MLIRNet_AttrOrTypeDefExtension {
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

extends Builtin_Float8E5M2 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E5M2\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E5M2Type(syntax))";
}

extends Builtin_Float8E4M3 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3Type(syntax))";
}

extends Builtin_Float8E4M3FN : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3FNType(syntax))";
}

extends Builtin_Float8E5M2FNUZ : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E5M2FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E5M2FNUZType(syntax))";
}

extends Builtin_Float8E4M3FNUZ : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3FNUZType(syntax))";
}

extends Builtin_Float8E4M3B11FNUZ : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E4M3B11FNUZ\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E4M3B11FNUZType(syntax))";
}

extends Builtin_Float8E3M4 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E3M4\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E3M4Type(syntax))";
}

extends Builtin_Float4E2M1FN : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f4E2M1FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float4E2M1FNType(syntax))";
}

extends Builtin_Float6E2M3FN : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f6E2M3FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float6E2M3FNType(syntax))";
}

extends Builtin_Float6E3M2FN : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f6E3M2FN\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float6E3M2FNType(syntax))";
}

extends Builtin_Float8E8M0FNU : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f8E8M0FNU\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float8E8M0FNUType(syntax))";
}

extends Builtin_BFloat16 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"bf16\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.BFloat16Type(syntax))";
}

extends Builtin_Float16 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f16\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float16Type(syntax))";
}

extends Builtin_FloatTF32 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"tf32\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.FloatTF32Type(syntax))";
}

extends Builtin_Float32 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f32\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float32Type(syntax))";
}

extends Builtin_Float64 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f64\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float64Type(syntax))";
}

extends Builtin_Float80 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f80\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float80Type(syntax))";
}

extends Builtin_Float128 : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"f128\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinScalarFloatTypeAssemblyFormat(syntax => new global::MLIR.Dialects.Builtin.Float128Type(syntax))";
}

// Assembly formats and C# names for scalar non-float types (index, none).

extends Builtin_Index : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"index\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinIndexTypeAssemblyFormat()";
}

extends Builtin_None : MLIRNet_AttrOrTypeDefExtension {
  let csharpName = "\"none\"";
  let csharpAssemblyFormat = "new global::MLIR.Dialects.Builtin.BuiltinNoneTypeAssemblyFormat()";
}

#endif // BUILTIN_TYPES_EXTENSIONS_TD
