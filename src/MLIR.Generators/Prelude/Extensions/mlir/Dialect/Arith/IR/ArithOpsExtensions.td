include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef ARITH_OPS_EXTENSIONS_TD
#define ARITH_OPS_EXTENSIONS_TD

extends SelectOp : CSharpOpExtension {
  let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance";
}

#endif // ARITH_OPS_EXTENSIONS_TD
