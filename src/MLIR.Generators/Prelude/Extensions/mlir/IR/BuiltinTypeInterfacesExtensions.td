include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD
#define BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD

extends ShapedTypeInterface : CSharpInterfaceExtension {
  let csharpName = "IShapedType";
  let csharpMembers = [
    CSharpInterfaceProperty<"getElementType", "global::MLIR.Semantics.TypeReference", "ElementType">,
    CSharpInterfaceProperty<"hasRank", "bool", "HasRank">,
    CSharpInterfaceProperty<"getShape", "global::System.Collections.Generic.IReadOnlyList<long>", "Shape">
  ];
}

#endif // BUILTIN_TYPE_INTERFACES_EXTENSIONS_TD
