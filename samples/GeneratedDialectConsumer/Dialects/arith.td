include "mlir/IR/OpBase.td"

class MiniArith_Op<string mnemonic, list<Trait> traits = []> :
    Op<MiniArith_Dialect, mnemonic, traits>;

def MiniArith_Dialect : Dialect {
  let name = "miniarith";
  let cppNamespace = "::mlir::miniarith";
  let summary = "Arithmetic dialect";
  let description = [{
    This dialect defines basic integer and floating point arithmetic ops.
  }];
  let hasConstantMaterializer = 1;
};

def MiniArith_ConstantOp : MiniArith_Op<"constant", [Pure]> {
  let summary = "integer constant";

  let arguments = (ins I32Attr:$value);
  let results = (outs I32:$result);

  let assemblyFormat = "$value attr-dict";
};

def MiniArith_AddIOp : MiniArith_Op<"addi", [Pure, Commutative]> {
  let summary = "integer addition";

  let arguments = (ins I32:$lhs, I32:$rhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$lhs `,` $rhs attr-dict `:` type($result)";
};
