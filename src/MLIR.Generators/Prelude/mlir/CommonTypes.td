// Common integer and floating-point type constraints for use in operation operands and results.
include "mlir/OpBase.td"

// Integer types
def I1 : Trait;
def I8 : Trait;
def I16 : Trait;
def I32 : Trait;
def I64 : Trait;
def I128 : Trait;

// Floating-point types
def BF16 : Trait;
def F16 : Trait;
def F32 : Trait;
def F64 : Trait;
def F80 : Trait;
def F128 : Trait;

// Other common types
def Index : Trait;
def Complex : Trait;
