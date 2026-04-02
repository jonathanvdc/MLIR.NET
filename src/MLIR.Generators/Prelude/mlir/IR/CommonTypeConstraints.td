// Common type constraints for use in operation operands and results.
// Compatible subset of mlir/include/mlir/IR/CommonTypeConstraints.td.

#ifndef COMMON_TYPE_CONSTRAINTS_TD
#define COMMON_TYPE_CONSTRAINTS_TD

include "mlir/IR/DialectBase.td"

// Signless integer types.
def I1   : Trait;
def I8   : Trait;
def I16  : Trait;
def I32  : Trait;
def I64  : Trait;
def I128 : Trait;

// Signed integer types.
def SI8  : Trait;
def SI16 : Trait;
def SI32 : Trait;
def SI64 : Trait;

// Unsigned integer types.
def UI1  : Trait;
def UI8  : Trait;
def UI16 : Trait;
def UI32 : Trait;
def UI64 : Trait;

// Floating-point types.
def BF16 : Trait;
def F16  : Trait;
def F32  : Trait;
def F64  : Trait;
def F80  : Trait;
def F128 : Trait;
def TF32 : Trait;

// 8-bit floating-point types.
def F8E4M3FN   : Trait;
def F8E5M2     : Trait;
def F8E4M3     : Trait;
def F8E4M3FNUZ : Trait;
def F8E5M2FNUZ : Trait;
def F8E8M0FNU  : Trait;

// Index type.
def Index : Trait;

// Complex type.
def AnyComplex : Trait;

// Any type constraints.
def AnyType              : Trait;
def AnyInteger           : Trait;
def AnyFloat             : Trait;
def AnySignlessInteger   : Trait;
def AnySignedInteger     : Trait;
def AnyUnsignedInteger   : Trait;
def AnySignlessIntegerOrIndex : Trait;
def NoneType             : Trait;

#endif // COMMON_TYPE_CONSTRAINTS_TD
