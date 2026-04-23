include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD
#define BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD

extends ShapedTypeInterface : MLIRNet_InterfaceExtension {
  let csharpName = "IShapedType";
  let csharpMembers = [
    MLIRNet_InterfaceProperty<"getElementType", "global::MLIR.Semantics.TypeReference", "ElementType">,
    MLIRNet_InterfaceProperty<"hasRank", "bool", "HasRank">,
    MLIRNet_InterfaceProperty<"getShape", "global::System.Collections.Generic.IReadOnlyList<long>", "Shape">
  ];
}

#endif // BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD
