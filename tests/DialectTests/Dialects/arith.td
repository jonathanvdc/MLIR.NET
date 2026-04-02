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

def MiniArith_AddImmediateOp : MiniArith_Op<"add_immediate", [Pure]> {
  let summary = "integer addition with an immediate";

  let arguments = (ins I32Attr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddBoolImmediateOp : MiniArith_Op<"add_bool_immediate", [Pure]> {
  let summary = "integer addition with a boolean immediate";

  let arguments = (ins BoolAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddFloatImmediateOp : MiniArith_Op<"add_float_immediate", [Pure]> {
  let summary = "integer addition with a float immediate";

  let arguments = (ins F32Attr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddStringImmediateOp : MiniArith_Op<"add_string_immediate", [Pure]> {
  let summary = "integer addition with a string immediate";

  let arguments = (ins StrAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddTypeImmediateOp : MiniArith_Op<"add_type_immediate", [Pure]> {
  let summary = "integer addition with a type immediate";

  let arguments = (ins TypeAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddUnitImmediateOp : MiniArith_Op<"add_unit_immediate", [Pure]> {
  let summary = "integer addition with a unit immediate";

  let arguments = (ins UnitAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddArrayImmediateOp : MiniArith_Op<"add_array_immediate", [Pure]> {
  let summary = "integer addition with a dense array immediate";

  let arguments = (ins DenseI32ArrayAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddElementsImmediateOp : MiniArith_Op<"add_elements_immediate", [Pure]> {
  let summary = "integer addition with an elements immediate";

  let arguments = (ins AnyI32ElementsAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};

def MiniArith_AddDictionaryImmediateOp : MiniArith_Op<"add_dictionary_immediate", [Pure]> {
  let summary = "integer addition with a dictionary immediate";

  let arguments = (ins DictionaryAttr:$value, I32:$lhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$value `,` $lhs attr-dict `:` type($result)";
};
