//===-- CommonAttrConstraints.td - Common Attr Constraints--*- tablegen -*-===//
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
//===----------------------------------------------------------------------===//

#ifndef COMMON_ATTR_CONSTRAINTS_TD
#define COMMON_ATTR_CONSTRAINTS_TD

include "mlir/IR/Constraints.td"
include "mlir/IR/CommonTypeConstraints.td"
include "mlir/IR/DialectBase.td"

//===----------------------------------------------------------------------===//
// Base attribute definition
//===----------------------------------------------------------------------===//

// Base class for all attributes.
class Attr<Pred condition, string summary = ""> :
    AttrConstraint<condition, summary> {
  code storageType = ?;
  code returnType = ?;
  code convertFromStorage = "$_self.getValue()";
  string constBuilderCall = ?;
  string defaultValue = ?;
  Type valueType = ?;
  bit isOptional = 0;
  Attr baseAttr = ?;
  string cppNamespace = "";
  string description = "";
}

// An attribute of a specific dialect.
class DialectAttr<Dialect d, Pred condition, string summary = ""> :
    Attr<condition, summary> {
  Dialect dialect = d;
  let cppNamespace = d.cppNamespace;
}

//===----------------------------------------------------------------------===//
// Attribute modifier definitions
//===----------------------------------------------------------------------===//

// Decorates an attribute to have an (unvalidated) default value if not present.
class DefaultValuedAttr<Attr attr, string val> :
    Attr<attr.predicate, attr.summary> {
  let storageType = attr.storageType;
  let returnType = attr.returnType;
  let convertFromStorage = attr.convertFromStorage;
  let constBuilderCall = attr.constBuilderCall;
  let defaultValue = val;
  let valueType = attr.valueType;
  let baseAttr = attr;
}

// Decorates an optional attribute to have an (unvalidated) default value.
class DefaultValuedOptionalAttr<Attr attr, string val> :
    Attr<attr.predicate, attr.summary> {
  let storageType = attr.storageType;
  let returnType = attr.returnType;
  let convertFromStorage = attr.convertFromStorage;
  let constBuilderCall = attr.constBuilderCall;
  let defaultValue = val;
  let valueType = attr.valueType;
  let isOptional = 1;
  let baseAttr = attr;
}

// Decorates an attribute as optional.
class OptionalAttr<Attr attr> : Attr<attr.predicate, attr.summary> {
  let storageType = attr.storageType;
  let returnType = "::std::optional<" # attr.returnType # ">";
  let convertFromStorage = "$_self ? " # returnType # "(" #
                           attr.convertFromStorage # ") : (::std::nullopt)";
  let valueType = attr.valueType;
  let isOptional = 1;
  let baseAttr = attr;
}

// Default-valued string-based attribute.
class DefaultValuedStrAttr<Attr attr, string val>
    : DefaultValuedAttr<attr, "\"" # val # "\"">;
class DefaultValuedOptionalStrAttr<Attr attr, string val>
    : DefaultValuedOptionalAttr<attr, "\"" # val # "\"">;

//===----------------------------------------------------------------------===//
// Primitive attribute kinds
//===----------------------------------------------------------------------===//

// A generic attribute that must be constructed around a specific buildable type.
class TypedAttrBase<Type attrValType, string attrKind, Pred condition,
                    string descr> :
    Attr<condition, descr> {
  let constBuilderCall = "$_builder.get" # attrKind # "(" #
                         attrValType.builderCall # ", $0)";
  let storageType = "::mlir::" # attrKind;
  let valueType = attrValType;
}

// Any attribute.
def AnyAttr : Attr<CPred<"true">, "any attribute"> {
  let storageType = "::mlir::Attribute";
  let returnType = "::mlir::Attribute";
  let convertFromStorage = "$_self";
  let constBuilderCall = "$0";
}

// Any attribute from the given list
class AnyAttrOf<list<Attr> allowedAttrs, string summary = "",
                string cppType = "::mlir::Attribute",
                string fromStorage = "$_self"> : Attr<
    Or<!foreach(allowedattr, allowedAttrs, allowedattr.predicate)>,
    !if(!eq(summary, ""),
        !interleave(!foreach(t, allowedAttrs, t.summary), " or "),
        summary)> {
    let returnType = cppType;
    let convertFromStorage = fromStorage;
    list<Attr> allowedAttributes = allowedAttrs;
}

def LocationAttr : Attr<CPred<"::llvm::isa<::mlir::LocationAttr>($_self)">,
                        "location attribute"> {
  let storageType = [{ ::mlir::LocationAttr }];
  let returnType = [{ ::mlir::Location }];
  let convertFromStorage = "::mlir::Location($_self)";
  let constBuilderCall = "(::mlir::LocationAttr)$0";
}

def BoolAttr : Attr<CPred<"::llvm::isa<::mlir::BoolAttr>($_self)">, "bool attribute"> {
  let storageType = [{ ::mlir::BoolAttr }];
  let returnType = [{ bool }];
  let valueType = I1;
  let constBuilderCall = "$_builder.getBoolAttr($0)";
}

// Index attribute.
def IndexAttr :
    TypedAttrBase<
      Index, "IntegerAttr",
      And<[CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
           CPred<"::llvm::isa<::mlir::IndexType>(::llvm::cast<::mlir::IntegerAttr>($_self).getType())">]>,
      "index attribute"> {
  let returnType = [{ ::llvm::APInt }];
}

// Base class for any integer attributes of fixed width.
class AnyIntegerAttrBase<AnyI attrValType, string descr> :
    TypedAttrBase<
      attrValType, "IntegerAttr",
      And<[CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
           CPred<"::llvm::cast<::mlir::IntegerAttr>($_self).getType()."
                 "isInteger(" # attrValType.bitwidth # ")">]>,
      descr> {
  let returnType = [{ ::llvm::APInt }];
  let constBuilderCall = ?;
}

def AnyI1Attr  : AnyIntegerAttrBase<AnyI1,  "1-bit integer attribute">;
def AnyI8Attr  : AnyIntegerAttrBase<AnyI8,  "8-bit integer attribute">;
def AnyI16Attr : AnyIntegerAttrBase<AnyI16, "16-bit integer attribute">;
def AnyI32Attr : AnyIntegerAttrBase<AnyI32, "32-bit integer attribute">;
def AnyI64Attr : AnyIntegerAttrBase<AnyI64, "64-bit integer attribute">;

def APIntAttr : Attr<CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
                     "arbitrary integer attribute"> {
  let storageType = [{ ::mlir::IntegerAttr }];
  let returnType = [{ ::mlir::APInt }];
}

// Base class for signless integer attributes of fixed width.
class SignlessIntegerAttrBase<I attrValType, string descr> :
    TypedAttrBase<
      attrValType, "IntegerAttr",
      And<[CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
           CPred<"::llvm::cast<::mlir::IntegerAttr>($_self).getType()."
                 "isSignlessInteger(" # attrValType.bitwidth # ")">]>,
      descr> {
  let returnType = [{ ::llvm::APInt }];
}

class TypedSignlessIntegerAttrBase<I attrValType, string retType, string descr>
    : SignlessIntegerAttrBase<attrValType, descr> {
  let returnType = retType;
  let convertFromStorage = "$_self.getValue().getZExtValue()";
}

def I1Attr  : TypedSignlessIntegerAttrBase<I1,  "bool",     "1-bit signless integer attribute">;
def I8Attr  : TypedSignlessIntegerAttrBase<I8,  "uint8_t",  "8-bit signless integer attribute">;
def I16Attr : TypedSignlessIntegerAttrBase<I16, "uint16_t", "16-bit signless integer attribute">;
def I32Attr : TypedSignlessIntegerAttrBase<I32, "uint32_t", "32-bit signless integer attribute">;
def I64Attr : TypedSignlessIntegerAttrBase<I64, "uint64_t", "64-bit signless integer attribute">;

// Base class for signed integer attributes of fixed width.
class SignedIntegerAttrBase<SI attrValType, string descr> :
    TypedAttrBase<
      attrValType, "IntegerAttr",
      And<[CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
           CPred<"::llvm::cast<::mlir::IntegerAttr>($_self).getType()."
                 "isSignedInteger(" # attrValType.bitwidth # ")">]>,
      descr> {
  let returnType = [{ ::llvm::APInt }];
}

class TypedSignedIntegerAttrBase<SI attrValType, string retType, string descr>
    : SignedIntegerAttrBase<attrValType, descr> {
  let returnType = retType;
  let convertFromStorage = "$_self.getValue().getSExtValue()";
}

def SI1Attr  : TypedSignedIntegerAttrBase<SI1,  "bool",    "1-bit signed integer attribute">;
def SI8Attr  : TypedSignedIntegerAttrBase<SI8,  "int8_t",  "8-bit signed integer attribute">;
def SI16Attr : TypedSignedIntegerAttrBase<SI16, "int16_t", "16-bit signed integer attribute">;
def SI32Attr : TypedSignedIntegerAttrBase<SI32, "int32_t", "32-bit signed integer attribute">;
def SI64Attr : TypedSignedIntegerAttrBase<SI64, "int64_t", "64-bit signed integer attribute">;

// Base class for unsigned integer attributes of fixed width.
class UnsignedIntegerAttrBase<UI attrValType, string descr> :
    TypedAttrBase<
      attrValType, "IntegerAttr",
      And<[CPred<"::llvm::isa<::mlir::IntegerAttr>($_self)">,
           CPred<"::llvm::cast<::mlir::IntegerAttr>($_self).getType()."
                 "isUnsignedInteger(" # attrValType.bitwidth # ")">]>,
      descr> {
  let returnType = [{ ::llvm::APInt }];
}

class TypedUnsignedIntegerAttrBase<UI attrValType, string retType, string descr>
    : UnsignedIntegerAttrBase<attrValType, descr> {
  let returnType = retType;
  let convertFromStorage = "$_self.getValue().getZExtValue()";
}

def UI1Attr  : TypedUnsignedIntegerAttrBase<UI1,  "bool",     "1-bit unsigned integer attribute">;
def UI8Attr  : TypedUnsignedIntegerAttrBase<UI8,  "uint8_t",  "8-bit unsigned integer attribute">;
def UI16Attr : TypedUnsignedIntegerAttrBase<UI16, "uint16_t", "16-bit unsigned integer attribute">;
def UI32Attr : TypedUnsignedIntegerAttrBase<UI32, "uint32_t", "32-bit unsigned integer attribute">;
def UI64Attr : TypedUnsignedIntegerAttrBase<UI64, "uint64_t", "64-bit unsigned integer attribute">;

// Base class for float attributes of fixed width.
class FloatAttrBase<F attrValType, string descr> :
    TypedAttrBase<attrValType, "FloatAttr",
              And<[CPred<"::llvm::isa<::mlir::FloatAttr>($_self)">,
                     CPred<"::llvm::cast<::mlir::FloatAttr>($_self).getType().isF" #
                           attrValType.bitwidth # "()">]>,
              descr> {
  let returnType = [{ ::llvm::APFloat }];
}

def F16Attr : FloatAttrBase<F16, "16-bit float attribute">;
def F32Attr : FloatAttrBase<F32, "32-bit float attribute">;
def F64Attr : FloatAttrBase<F64, "64-bit float attribute">;

def BF16Attr : TypedAttrBase<BF16, "FloatAttr",
              And<[CPred<"::llvm::isa<::mlir::FloatAttr>($_self)">,
                     CPred<"::llvm::cast<::mlir::FloatAttr>($_self).getType().isBF16()">]>,
              "16-bit bfloat attribute"> {
  let returnType = [{ ::llvm::APFloat }];
}

// An attribute backed by a string type.
class StringBasedAttr<Pred condition, string descr> : Attr<condition, descr> {
  let constBuilderCall = "$_builder.getStringAttr($0)";
  let storageType = [{ ::mlir::StringAttr }];
  let returnType = [{ ::llvm::StringRef }];
  let valueType = NoneType;
}

def StrAttrPred : CPred<"::llvm::isa<::mlir::StringAttr>($_self)">;

def StrAttr : StringBasedAttr<StrAttrPred, "string attribute">;

// A string attribute that represents the name of a symbol.
def SymbolNameAttr : StringBasedAttr<StrAttrPred, "string attribute">;

// Type attribute.
def TypeAttr : Attr<CPred<"::llvm::isa<::mlir::TypeAttr>($_self)">, "type attribute"> {
  let storageType = [{ ::mlir::TypeAttr }];
  let returnType = [{ ::mlir::Type }];
  let convertFromStorage = "$_self.getValue()";
}

// Unit attribute.
def UnitAttr : Attr<CPred<"::llvm::isa<::mlir::UnitAttr>($_self)">, "unit attribute"> {
  let storageType = [{ ::mlir::UnitAttr }];
  let returnType = [{ bool }];
  let constBuilderCall = "(($0) ? $_builder.getUnitAttr() : ::mlir::UnitAttr())";
  let convertFromStorage = "$_self ? true : false";
  let valueType = NoneType;
}

// Dense array attributes.
class DenseArrayAttrBase<string denseArrayTy, string cppTy, string descr> :
    Attr<CPred<"::llvm::isa<::mlir::" # denseArrayTy # ">($_self)">, descr> {
  let storageType = "::mlir::" # denseArrayTy;
  let returnType = "::llvm::ArrayRef<" # cppTy # ">";
  let convertFromStorage = "::llvm::ArrayRef<" # cppTy # ">($_self)";
}

def DenseBoolArrayAttr : DenseArrayAttrBase<"DenseBoolArrayAttr", "bool", "i1 dense array attribute">;
def DenseI8ArrayAttr   : DenseArrayAttrBase<"DenseI8ArrayAttr",   "int8_t",   "i8 dense array attribute">;
def DenseI16ArrayAttr  : DenseArrayAttrBase<"DenseI16ArrayAttr",  "int16_t",  "i16 dense array attribute">;
def DenseI32ArrayAttr  : DenseArrayAttrBase<"DenseI32ArrayAttr",  "int32_t",  "i32 dense array attribute">;
def DenseI64ArrayAttr  : DenseArrayAttrBase<"DenseI64ArrayAttr",  "int64_t",  "i64 dense array attribute">;
def DenseF32ArrayAttr  : DenseArrayAttrBase<"DenseF32ArrayAttr",  "float",    "f32 dense array attribute">;
def DenseF64ArrayAttr  : DenseArrayAttrBase<"DenseF64ArrayAttr",  "double",   "f64 dense array attribute">;

// Elements attributes.
def ElementsAttr : Attr<CPred<"::llvm::isa<::mlir::ElementsAttr>($_self)">, "elements attribute"> {
  let storageType = [{ ::mlir::ElementsAttr }];
  let returnType = [{ ::mlir::ElementsAttr }];
  let convertFromStorage = "$_self";
}

def AnyIntElementsAttr : Attr<
    And<[CPred<"::llvm::isa<::mlir::ElementsAttr>($_self)">,
         CPred<"::llvm::cast<::mlir::ElementsAttr>($_self).getElementType().isInteger()">]>,
    "integer elements attribute"> {
  let storageType = [{ ::mlir::ElementsAttr }];
  let returnType = [{ ::mlir::ElementsAttr }];
  let convertFromStorage = "$_self";
}

def AnyI32ElementsAttr : Attr<
    And<[CPred<"::llvm::isa<::mlir::ElementsAttr>($_self)">,
         CPred<"::llvm::cast<::mlir::ElementsAttr>($_self).getElementType().isInteger(32)">]>,
    "i32 elements attribute"> {
  let storageType = [{ ::mlir::ElementsAttr }];
  let returnType = [{ ::mlir::ElementsAttr }];
  let convertFromStorage = "$_self";
}

def AnyI64ElementsAttr : Attr<
    And<[CPred<"::llvm::isa<::mlir::ElementsAttr>($_self)">,
         CPred<"::llvm::cast<::mlir::ElementsAttr>($_self).getElementType().isInteger(64)">]>,
    "i64 elements attribute"> {
  let storageType = [{ ::mlir::ElementsAttr }];
  let returnType = [{ ::mlir::ElementsAttr }];
  let convertFromStorage = "$_self";
}

// Dictionary attribute.
def DictionaryAttr : Attr<CPred<"::llvm::isa<::mlir::DictionaryAttr>($_self)">,
                          "dictionary of named attribute values"> {
  let storageType = [{ ::mlir::DictionaryAttr }];
  let returnType = [{ ::mlir::DictionaryAttr }];
  let convertFromStorage = "$_self";
  let constBuilderCall = "$_builder.getDictionaryAttr($0)";
}

#endif // COMMON_ATTR_CONSTRAINTS_TD
