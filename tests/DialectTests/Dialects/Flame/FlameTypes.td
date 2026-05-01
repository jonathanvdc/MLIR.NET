#ifndef FLAME_TYPES
#define FLAME_TYPES

include "mlir/IR/AttrTypeBase.td"
include "FlameDialect.td"

class Flame_Type<string name, string mnemonic>
    : TypeDef<Flame_Dialect, name> {
  let mnemonic = mnemonic;
}

def Flame_PointerKindAttr : AttrDef<Flame_Dialect, "PointerKind"> {
  let mnemonic = "ptr_kind";
  let summary = "Pointer kind";
  let parameters = (ins
    StrAttr:$value
  );
  let assemblyFormat = "`<` $value `>`";
}

// flame.ptr<elem, "Box"|"Reference"|"Transient">
def Flame_PointerType : Flame_Type<"PointerType", "ptr"> {
  let summary = "Pointer type";
  let parameters = (ins
    "mlir::Type":$elementType,
    "mlir::StringAttr":$kind
  );
  let assemblyFormat = "`<` $elementType `,` $kind `>`";
}

// flame.specialized<decl, [args...]>
def Flame_SpecializedType : Flame_Type<"SpecializedType", "specialized"> {
  let summary = "Direct type specialization";
  let parameters = (ins
    "mlir::Type":$declaration,
    ArrayRefParameter<"mlir::Type">:$genericArguments
  );
  let assemblyFormat = "`<` $declaration `,` `[` $genericArguments `]` `>`";
}

// flame.generic_param<owner, name>
def Flame_GenericParamType : Flame_Type<"GenericParamType", "generic_param"> {
  let summary = "Generic parameter reference";
  let parameters = (ins
    "mlir::FlatSymbolRefAttr":$owner,
    "mlir::StringAttr":$name
  );
  let assemblyFormat = "`<` $owner `,` $name `>`";
}

// flame.named<@sym>
def Flame_NamedType : Flame_Type<"NamedType", "named"> {
  let summary = "Named type reference";
  let parameters = (ins
    "mlir::FlatSymbolRefAttr":$sym
  );
  let assemblyFormat = "`<` $sym `>`";
}

// flame.nested<parent, "Child">
def Flame_NestedType : Flame_Type<"NestedType", "nested"> {
  let summary = "Nested type reference";
  let parameters = (ins
    "mlir::Type":$parent,
    "mlir::StringAttr":$name
  );
  let assemblyFormat = "`<` $parent `,` $name `>`";
}

#endif // FLAME_TYPES