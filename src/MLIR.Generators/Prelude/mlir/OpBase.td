// Core ODS class definitions compatible with the MLIR.NET evaluator.
// This file defines the fundamental base classes and common traits used in ODS dialects.

#ifndef OPBASE_TD
#define OPBASE_TD

include "mlir/IR/DialectBase.td"
include "mlir/IR/CommonTypeConstraints.td"
include "mlir/IR/CommonAttrConstraints.td"

class Op<Dialect dialect, string mnemonic, list<Trait> traits = []> {
  Dialect dialect = dialect;
  string mnemonic = mnemonic;
  list<Trait> traits = traits;
  string summary = "";
  string assemblyFormat = "";
  string cppClassName = "";
}

class AttrDef<Dialect dialect, string attrName> {
  Dialect dialect = dialect;
  string attrName = attrName;
  string cppClassName = "";
}

class TypeDef<Dialect dialect, string typeName> {
  Dialect dialect = dialect;
  string typeName = typeName;
  string cppClassName = "";
}

#endif // OPBASE_TD
