// Common attribute constraints for use in operation arguments.
include "mlir/OpBase.td"

// Integer attribute constraints
def I1Attr : Trait;
def I8Attr : Trait;
def I16Attr : Trait;
def I32Attr : Trait;
def I64Attr : Trait;

// Floating-point attribute constraints
def BF16Attr : Trait;
def F16Attr : Trait;
def F32Attr : Trait;
def F64Attr : Trait;

// Other common attribute constraints
def IndexAttr : Trait;
def StringAttr : Trait;
def BoolAttr : Trait;
def UnitAttr : Trait;
