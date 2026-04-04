include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef ARITH_OPS_EXTENSIONS_TD
#define ARITH_OPS_EXTENSIONS_TD

def MLIRNet_Arith_SelectOpAssemblyExtension : MLIRNet_OpExtension<"arith.select"> {
    let csharpAsmFormatCode = "global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance";
}

#endif // ARITH_OPS_EXTENSIONS_TD