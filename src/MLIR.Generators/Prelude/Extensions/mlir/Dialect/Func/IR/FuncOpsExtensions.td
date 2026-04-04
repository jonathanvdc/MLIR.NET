include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef FUNC_OPS_EXTENSIONS_TD
#define FUNC_OPS_EXTENSIONS_TD

def MLIRNet_Func_FuncOpAssemblyExtension : MLIRNet_OpExtension<"func.func"> {
  let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.FuncOperationAssemblyFormat.Instance";
}

#endif // FUNC_OPS_EXTENSIONS_TD
