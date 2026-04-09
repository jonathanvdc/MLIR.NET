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

#endif // BUILTIN_TYPES_EXTENSIONS_TD
