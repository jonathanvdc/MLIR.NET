include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef FUNC_OPS_EXTENSIONS_TD
#define FUNC_OPS_EXTENSIONS_TD

extends FuncOp : CSharpOpExtension {
  let csharpAsmFormatCode = "new global::MLIR.Dialects.Extensions.FuncOperationAssemblyFormat($_definition)";
}

#endif // FUNC_OPS_EXTENSIONS_TD
