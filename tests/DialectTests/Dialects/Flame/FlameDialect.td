#ifndef FLAME_DIALECT
#define FLAME_DIALECT

include "mlir/IR/DialectBase.td"

def Flame_Dialect : Dialect {
  let name = "flame";
  let cppNamespace = "::mlir::flame";

  let summary = "Flame IR dialect";
  let description = [{
    A dialect that models the Flame IR entities visible in the uploaded
    codecs: type references, constants, fields/methods, and instruction
    prototypes such as alloca, call, load, store, casts, etc.
  }];

  let hasConstantMaterializer = 1;
}

#endif // FLAME_DIALECT
