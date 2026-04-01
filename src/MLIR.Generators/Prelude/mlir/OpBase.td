// Core ODS class definitions compatible with the MLIR.NET evaluator.
// This file defines the fundamental base classes and common traits used in ODS dialects.

class Trait;

class Dialect {
  string name = "";
  string cppNamespace = "";
  string summary = "";
  string description = "";
  bit hasConstantMaterializer = 0;
}

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

// Common operation traits
def Pure : Trait;
def Commutative : Trait;
def NoMemoryEffect : Trait;
def Idempotent : Trait;
def Involution : Trait;
def SameOperandsAndResultType : Trait;
def SameOperandsAndResultShape : Trait;
def SameTypeOperands : Trait;
def ElementwiseMappable : Trait;
