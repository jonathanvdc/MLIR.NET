include "mlir/IR/OpBase.td"
include "mlir/IR/EnumAttr.td"

class MiniEnum_Op<string mnemonic, list<Trait> traits = []> :
    Op<MiniEnum_Dialect, mnemonic, traits>;

def MiniEnum_Dialect : Dialect {
  let name = "minienum";
  let cppNamespace = "::mlir::minienum";
};

def MINI_MODE_A : I32EnumAttrCase<"a", 0>;
def MINI_MODE_B : I32EnumAttrCase<"b", 1>;

def MiniEnum_Mode : I32EnumAttr<"Mode", "Test mode", [MINI_MODE_A, MINI_MODE_B]> {
  let cppNamespace = "::mlir::minienum";
  let genSpecializedAttr = 0;
}

def MiniEnum_ModeAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Mode, "mode"> {
  let assemblyFormat = "`<` $value `>`";
}

def MINI_FLAG_NONE : I32BitEnumAttrCaseNone<"none">;
def MINI_FLAG_X : I32BitEnumAttrCaseBit<"x", 0>;
def MINI_FLAG_Y : I32BitEnumAttrCaseBit<"y", 1>;
def MINI_FLAG_XY : I32BitEnumAttrCaseGroup<"xy", [MINI_FLAG_X, MINI_FLAG_Y]>;

def MiniEnum_Flags : I32BitEnumAttr<"Flags", "Test flags", [MINI_FLAG_NONE, MINI_FLAG_X, MINI_FLAG_Y, MINI_FLAG_XY]> {
  let separator = ",";
  let cppNamespace = "::mlir::minienum";
  let genSpecializedAttr = 0;
  let printBitEnumPrimaryGroups = 1;
}

def MiniEnum_FlagsAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Flags, "flags"> {
  let assemblyFormat = "`<` $value `>`";
}

def MiniEnum_ModeOp : MiniEnum_Op<"mode_op", [Pure]> {
  let arguments = (ins MiniEnum_ModeAttr:$mode, I32:$input);
  let results = (outs I32:$result);
  let assemblyFormat = "$mode `,` $input attr-dict `:` type($result)";
}

def MiniEnum_FlagsOp : MiniEnum_Op<"flags_op", [Pure]> {
  let arguments = (ins MiniEnum_FlagsAttr:$flags, I32:$input);
  let results = (outs I32:$result);
  let assemblyFormat = "$flags $input attr-dict `:` type($result)";
}
