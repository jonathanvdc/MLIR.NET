#ifndef FLAME_SYMBOL_OPS
#define FLAME_SYMBOL_OPS

include "mlir/IR/OpBase.td"
include "mlir/IR/SymbolInterfaces.td"
include "FlameDialect.td"

def Flame_AssemblyOp : Op<Flame_Dialect, "assembly",
    [SymbolTable, IsolatedFromAbove]> {
  let summary = "Assembly symbol table";
  let arguments = (ins
    SymbolNameAttr:$sym_name
  );
  let regions = (region SizedRegion<1>:$body);
  let assemblyFormat = "$sym_name attr-dict $body";
}

def Flame_TypeOp : Op<Flame_Dialect, "type",
    [Symbol, SymbolTable]> {
  let summary = "Type definition";
  let arguments = (ins
    SymbolNameAttr:$sym_name,
    OptionalAttr<TypeAttr>:$declaredType,
    ArrayAttr:$bases
  );
  let regions = (region SizedRegion<1>:$body);
  let assemblyFormat = "$sym_name (`:` $declaredType^)? (`bases` `=` $bases^)? attr-dict $body";
}

def Flame_FieldOp : Op<Flame_Dialect, "field", [Symbol]> {
  let summary = "Field definition";
  let arguments = (ins
    SymbolNameAttr:$sym_name,
    TypeAttr:$type,
    I1Attr:$isStatic
  );
  let assemblyFormat = "$sym_name `:` $type `static` `=` $isStatic attr-dict";
}

def Flame_MethodOp : Op<Flame_Dialect, "method",
    [Symbol, FunctionOpInterface]> {
  let summary = "Method definition";
  let arguments = (ins
    SymbolNameAttr:$sym_name,
    TypeAttr:$functionType,
    I1Attr:$isStatic
  );
  let regions = (region SizedRegion<1>:$body);
  let assemblyFormat = "$sym_name `:` $functionType `static` `=` $isStatic attr-dict $body";
}

#endif // FLAME_SYMBOL_OPS