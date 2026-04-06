include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef FUNC_OPS_EXTENSIONS_TD
#define FUNC_OPS_EXTENSIONS_TD

extends FuncOp : MLIRNet_OpExtension {
  let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.FuncOperationAssemblyFormat.Instance";
};

#endif // FUNC_OPS_EXTENSIONS_TD
