include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef ARITH_OPS_EXTENSIONS_TD
#define ARITH_OPS_EXTENSIONS_TD

extends SelectOp : MLIRNet_OpExtension {
  let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance";
}

#endif // ARITH_OPS_EXTENSIONS_TD
