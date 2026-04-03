include "mlir/Extensions/IR/MLIRNetExtensions.td"

def MLIRNet_Arith_SelectOpAssemblyExtension
    : MLIRNet_OpAsmExtension<"arith.select", "select_like">;
