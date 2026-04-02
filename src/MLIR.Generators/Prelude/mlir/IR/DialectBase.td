// Base Dialect class definition.
// Compatible subset of mlir/include/mlir/IR/DialectBase.td.

#ifndef DIALECTBASE_TD
#define DIALECTBASE_TD

include "mlir/IR/Utils.td"

// Base class for all dialects.
class Dialect {
  // The name of the dialect.
  string name = "";

  // Short summary of the dialect.
  string summary = "";

  // The description of the dialect.
  string description = "";

  // A list of dialects this dialect will load on construction as dependencies.
  list<string> dependentDialects = [];

  // The C++ namespace that ops of this dialect should be placed into.
  string cppNamespace = "";

  // An optional code block containing extra declarations to place in the
  // dialect declaration.
  string extraClassDeclaration = "";

  // If this dialect overrides the hook for materializing constants.
  bit hasConstantMaterializer = 0;

  // If the dialect definition provides a non-default destructor.
  bit hasNonDefaultDestructor = 0;

  // If this dialect overrides the hook for verifying operation attributes.
  bit hasOperationAttrVerify = 0;

  // If this dialect overrides the hook for verifying region argument attributes.
  bit hasRegionArgAttrVerify = 0;

  // If this dialect overrides the hook for verifying region result attributes.
  bit hasRegionResultAttrVerify = 0;

  // If this dialect overrides the hook for op interface fallback.
  bit hasOperationInterfaceFallback = 0;

  // If this dialect should use default generated attribute parser boilerplate.
  bit useDefaultAttributePrinterParser = 0;

  // If this dialect should use default generated type parser boilerplate.
  bit useDefaultTypePrinterParser = 0;

  // If this dialect overrides the hook for canonicalization patterns.
  bit hasCanonicalizer = 0;

  // If this dialect can be extended at runtime with new operations or types.
  bit isExtensible = 0;
}

// Common operation traits.
class Trait;

def Pure : Trait;
def Commutative : Trait;
def NoMemoryEffect : Trait;
def Idempotent : Trait;
def Involution : Trait;
def SameOperandsAndResultType : Trait;
def SameOperandsAndResultShape : Trait;
def SameTypeOperands : Trait;
def ElementwiseMappable : Trait;

#endif // DIALECTBASE_TD
