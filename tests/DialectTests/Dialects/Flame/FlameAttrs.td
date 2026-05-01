#ifndef FLAME_ATTRS
#define FLAME_ATTRS

include "mlir/IR/AttrTypeBase.td"
include "FlameDialect.td"
include "FlameTypes.td"

class Flame_Attr<string name, string mnemonic>
    : AttrDef<Flame_Dialect, name> {
  let mnemonic = mnemonic;
}

def Flame_NullAttr : Flame_Attr<"NullAttr", "null"> {
  let summary = "Null constant";
  let assemblyFormat = "";
}

def Flame_DefaultAttr : Flame_Attr<"DefaultAttr", "default"> {
  let summary = "Default-value constant";
  let assemblyFormat = "";
}

def Flame_TypeTokenAttr : Flame_Attr<"TypeTokenAttr", "typeof"> {
  let summary = "Type token constant";
  let parameters = (ins "mlir::Type":$value);
  let assemblyFormat = "`<` $value `>`";
}

def Flame_FieldTokenAttr : Flame_Attr<"FieldTokenAttr", "fieldof"> {
  let summary = "Field token constant";
  let parameters = (ins "mlir::FlatSymbolRefAttr":$field);
  let assemblyFormat = "`<` $field `>`";
}

def Flame_MethodTokenAttr : Flame_Attr<"MethodTokenAttr", "methodof"> {
  let summary = "Method token constant";
  let parameters = (ins "mlir::FlatSymbolRefAttr":$method);
  let assemblyFormat = "`<` $method `>`";
}

#endif // FLAME_ATTRS