// Common attribute constraints for use in operation arguments.
// Compatible subset of mlir/include/mlir/IR/CommonAttrConstraints.td.

#ifndef COMMON_ATTR_CONSTRAINTS_TD
#define COMMON_ATTR_CONSTRAINTS_TD

include "mlir/IR/CommonTypeConstraints.td"

// Any attribute.
def AnyAttr : Trait;

// Boolean attribute.
def BoolAttr : Trait;

// Index attribute.
def IndexAttr : Trait;

// Signless integer attributes of fixed width.
def I1Attr  : Trait;
def I8Attr  : Trait;
def I16Attr : Trait;
def I32Attr : Trait;
def I64Attr : Trait;

// Signed integer attributes of fixed width.
def SI8Attr  : Trait;
def SI16Attr : Trait;
def SI32Attr : Trait;
def SI64Attr : Trait;

// Unsigned integer attributes of fixed width.
def UI8Attr  : Trait;
def UI16Attr : Trait;
def UI32Attr : Trait;
def UI64Attr : Trait;

// Float attributes.
def BF16Attr : Trait;
def F16Attr  : Trait;
def F32Attr  : Trait;
def F64Attr  : Trait;

// String attribute.
def StrAttr        : Trait;
def SymbolNameAttr : Trait;

// Type attribute.
def TypeAttr : Trait;

// Unit attribute.
def UnitAttr : Trait;

// Location attribute.
def LocationAttr : Trait;

// Arbitrary integer attribute.
def APIntAttr : Trait;

// Dense array attributes.
def DenseBoolArrayAttr : Trait;
def DenseI8ArrayAttr   : Trait;
def DenseI16ArrayAttr  : Trait;
def DenseI32ArrayAttr  : Trait;
def DenseI64ArrayAttr  : Trait;
def DenseF32ArrayAttr  : Trait;
def DenseF64ArrayAttr  : Trait;

// Elements attributes.
def ElementsAttr    : Trait;
def AnyIntElementsAttr : Trait;
def AnyI32ElementsAttr : Trait;
def AnyI64ElementsAttr : Trait;

// Dictionary attribute.
def DictionaryAttr : Trait;

#endif // COMMON_ATTR_CONSTRAINTS_TD
