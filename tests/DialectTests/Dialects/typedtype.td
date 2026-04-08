include "mlir/IR/OpBase.td"
include "mlir/IR/AttrTypeBase.td"

class TypedType_Op<string mnemonic, list<Trait> traits = []> :
    Op<TypedType_Dialect, mnemonic, traits>;

def TypedType_Dialect : Dialect {
  let name = "typedtype";
  let cppNamespace = "::mlir::typedtype";
};

class TypedType_Type<string name> : TypeDef<TypedType_Dialect, name> {
  let typeName = "typed." # name;
  let cppClassName = "OpaqueType";
};

def TypedType_OpaqueType : TypedType_Type<"opaque"> {
  let summary = "an opaque generated type";
  let description = [{
    A type used to verify generated type round-tripping in consumer tests.
  }];
  let parameters = (ins StringRefParameter<"payload">:$value);
  let assemblyFormat = "`<` $value `>`";
};

def TypedType_RoundTripOp : TypedType_Op<"round_trip", []> {
  let results = (outs TypedType_OpaqueType:$result);
  let assemblyFormat = "attr-dict `:` type($result)";
};
