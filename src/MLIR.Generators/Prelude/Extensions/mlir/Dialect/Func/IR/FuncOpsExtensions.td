include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef FUNC_OPS_EXTENSIONS_TD
#define FUNC_OPS_EXTENSIONS_TD

extends FuncOp : CSharpOpExtension {
  let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.FuncOperationAssemblyFormat.Instance";
}

#endif // FUNC_OPS_EXTENSIONS_TD
