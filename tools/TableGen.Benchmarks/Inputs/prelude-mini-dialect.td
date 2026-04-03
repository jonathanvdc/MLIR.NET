include "mlir/IR/OpBase.td"

class MiniArith_Op<string mnemonic, list<Trait> traits = []> : Op<MiniArith_Dialect, mnemonic, traits>;
def MiniArith_Dialect : Dialect {
  let name = "miniarith";
};
def MiniArith_AddIOp : MiniArith_Op<"addi", [Pure, Commutative]> {
  let arguments = (ins I32:$lhs, I32:$rhs);
};
