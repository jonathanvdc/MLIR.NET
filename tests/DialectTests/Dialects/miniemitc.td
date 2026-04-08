include "mlir/IR/OpBase.td"
include "mlir/IR/AttrTypeBase.td"

class MiniEmitC_Op<string mnemonic, list<Trait> traits = []> :
    Op<MiniEmitC_Dialect, mnemonic, traits>;

def MiniEmitC_Dialect : Dialect {
  let name = "miniemitc";
  let cppNamespace = "::mlir::miniemitc";
};

// Base class for miniemitc attribute definitions.
class MiniEmitC_Attr<string name, string attrMnemonic, list<Trait> traits = []>
    : AttrDef<MiniEmitC_Dialect, name, traits> {
  let mnemonic = attrMnemonic;
}

// An opaque attribute whose value is emitted verbatim.
//
// Example:
//   #miniemitc.opaque<"NULL">
//   #miniemitc.opaque<"nullptr">
def MiniEmitC_OpaqueAttr : MiniEmitC_Attr<"Opaque", "opaque"> {
  let summary = "An opaque attribute";

  let parameters = (ins StringRefParameter<"the opaque value">:$value);
  let assemblyFormat = "`<` $value `>`";
}

// An attribute holding a pair of integer parameters.
//
// Example:
//   #miniemitc.pair<42, 7>
def MiniEmitC_PairAttr : MiniEmitC_Attr<"Pair", "pair"> {
  let summary = "A pair of integer values";

  let parameters = (ins APIntParameter<"the first value">:$first,
                        APIntParameter<"the second value">:$second);
  let assemblyFormat = "`<` $first `,` $second `>`";
}

// A simple operation that uses the opaque attribute.
def MiniEmitC_UseOpaqueOp : MiniEmitC_Op<"use_opaque", []> {
  let arguments = (ins MiniEmitC_OpaqueAttr:$attr);
  let results = (outs);
  let assemblyFormat = "$attr attr-dict";
}
