//===-- CommonTypeConstraints.td - Common Type Constraints--*- tablegen -*-===//
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
//===----------------------------------------------------------------------===//
//
// This file contains commonly used type constraints.
//
//===----------------------------------------------------------------------===//

#ifndef COMMON_TYPE_CONSTRAINTS_TD
#define COMMON_TYPE_CONSTRAINTS_TD

include "mlir/IR/Constraints.td"
include "mlir/IR/DialectBase.td"

//===----------------------------------------------------------------------===//
// Common predicates
//===----------------------------------------------------------------------===//

// Whether a type is a VectorType.
// Explicitly disallow 0-D vectors for now until we have good enough coverage.
def IsVectorOfNonZeroRankTypePred : And<[CPred<"::llvm::isa<::mlir::VectorType>($_self)">,
                                         CPred<"::llvm::cast<::mlir::VectorType>($_self).getRank() > 0">]>;
def IsFixedVectorOfNonZeroRankTypePred : And<[CPred<"::llvm::isa<::mlir::FixedVectorType>($_self)">,
                                              CPred<"::llvm::cast<::mlir::VectorType>($_self).getRank() > 0">]>;

// Temporary vector type clone that allows gradual transition to 0-D vectors.
def IsVectorOfAnyRankTypePred : CPred<"::llvm::isa<::mlir::VectorType>($_self)">;

// Whether a type is a fixed-length VectorType.
def IsFixedVectorOfAnyRankTypePred : CPred<[{::llvm::isa<::mlir::FixedVectorType>($_self)}]>;

// Whether a type is a scalable VectorType.
def IsVectorTypeWithAnyDimScalablePred
        : CPred<[{::llvm::isa<::mlir::ScalableVectorType>($_self)}]>;

// Whether a type is a TensorType.
def IsTensorTypePred : CPred<"::llvm::isa<::mlir::TensorType>($_self)">;

// Whether a type is a MemRefType.
def IsMemRefTypePred : CPred<"::llvm::isa<::mlir::MemRefType>($_self)">;

// Whether a type is a TensorType or a MemRefType.
def IsTensorOrMemRefTypePred : Or<[IsTensorTypePred, IsMemRefTypePred]>;

// Whether a type is an UnrankedMemRefType
def IsUnrankedMemRefTypePred
        : CPred<"::llvm::isa<::mlir::UnrankedMemRefType>($_self)">;

// Whether a type is an UnrankedTensorType
def IsUnrankedTensorTypePred
        : CPred<"::llvm::isa<::mlir::UnrankedTensorType>($_self)">;

// Whether a type is a RankedTensorType
def IsRankedTensorTypePred
        : CPred<"::llvm::isa<::mlir::RankedTensorType>($_self)">;

// Whether a type is a BaseMemRefType
def IsBaseMemRefTypePred
        : CPred<"::llvm::isa<::mlir::BaseMemRefType>($_self)">;

// Whether a type is a ShapedType.
def IsShapedTypePred : CPred<"::llvm::isa<::mlir::ShapedType>($_self)">;

// For a ShapedType, verify that it has a static shape.
def HasStaticShapePred :
        CPred<"::llvm::cast<::mlir::ShapedType>($_self).hasStaticShape()">;

// Whether a type is a TupleType.
def IsTupleTypePred : CPred<"::llvm::isa<::mlir::TupleType>($_self)">;

// Whether a type has a ValueSemantics trait.
def HasValueSemanticsPred : CPred<"$_self.hasTrait<::mlir::ValueSemantics>()">;

//===----------------------------------------------------------------------===//
// Type definitions
//===----------------------------------------------------------------------===//

// A type, carries type constraints.
class Type<Pred condition, string descr = "",
           string cppType = "::mlir::Type"> :
    TypeConstraint<condition, descr, cppType> {
  string description = "";
  string builderCall = "";
}

// Allows providing an alternative name and summary to an existing type def.
class TypeAlias<Type t, string summary = t.summary> :
    Type<t.predicate, summary, t.cppType> {
  let description = t.description;
  let builderCall = t.builderCall;
}

// A type of a specific dialect.
class DialectType<Dialect d, Pred condition, string descr = "",
                  string cppType = "::mlir::Type"> :
    Type<condition, descr, cppType> {
  Dialect dialect = d;
}

// A variadic type constraint. It expands to zero or more of the base type. This
// class is used for supporting variadic operands/results.
class Variadic<Type type> : TypeConstraint<type.predicate,
                                           "variadic of " # type.summary,
                                           type.cppType> {
  Type baseType = type;
  int minSize = 0;
}

// A nested variadic type constraint.
class VariadicOfVariadic<Type type, string variadicSegmentAttrName>
    : Variadic<type> {
  string segmentAttrName = variadicSegmentAttrName;
}

// An optional type constraint. It expands to either zero or one of the base
// type. This class is used for supporting optional operands/results.
class Optional<Type type> : TypeConstraint<type.predicate, type.summary,
                                           type.cppType> {
  Type baseType = type;
}

// A type that can be constructed using MLIR::Builder.
class BuildableType<code builder> {
  code builderCall = builder;
}

// A type that's buildable iff the type passed as an argument is buildable.
class SameBuildabilityAs<Type type, code builder> {
  code builderCall = !if(!empty(type.builderCall), "", builder);
}

// Any type at all.
def AnyType : Type<CPred<"true">, "any type">;

// None type
def NoneType : Type<CPred<"::llvm::isa<::mlir::NoneType>($_self)">, "none type",
                    "::mlir::NoneType">,
      BuildableType<"$_builder.getType<::mlir::NoneType>()">;

// Any type from the given list
class AnyTypeOf<list<Type> allowedTypeList, string summary = "",
                string cppType = "::mlir::Type"> : Type<
    // Satisfy any of the allowed types' conditions.
    Or<!foreach(allowedtype, allowedTypeList, allowedtype.predicate)>,
    !if(!eq(summary, ""),
        !interleave(!foreach(t, allowedTypeList, t.summary), " or "),
        summary),
    cppType> {
  list<Type> allowedTypes = allowedTypeList;
}

// A type that satisfies the constraints of all given types.
class AllOfType<list<Type> allowedTypeList, string summary = "",
                string cppType = "::mlir::Type"> : Type<
    // Satisfy all of the allowed types' conditions.
    And<!foreach(allowedType, allowedTypeList, allowedType.predicate)>,
    !if(!eq(summary, ""),
        !interleave(!foreach(t, allowedTypeList, t.summary), " and "),
        summary),
    cppType> {
  list<Type> allowedTypes = allowedTypeList;
}

// A type that satisfies additional predicates.
class ConfinedType<Type type, list<Pred> predicates, string summary = "",
                   string cppType = type.cppType> : Type<
    And<!listconcat([type.predicate], !foreach(pred, predicates, pred))>,
    summary, cppType> {
    Type baseType = type;
    list<Pred> predicateList = predicates;
}

//===----------------------------------------------------------------------===//
// Integer types
//===----------------------------------------------------------------------===//

// Any integer type irrespective of its width and signedness semantics.
def AnyInteger : Type<CPred<"::llvm::isa<::mlir::IntegerType>($_self)">, "integer",
                      "::mlir::IntegerType">;

// Any integer type (regardless of signedness semantics) of a specific width.
class AnyI<int width>
    : Type<CPred<"$_self.isInteger(" # width # ")">, width # "-bit integer"> {
  int bitwidth = width;
}

class AnyIntOfWidths<list<int> widths> :
    AnyTypeOf<!foreach(w, widths, AnyI<w>),
              !interleave(widths, "/") # "-bit integer",
              "::mlir::IntegerType">;

def AnyI1  : AnyI<1>;
def AnyI8  : AnyI<8>;
def AnyI16 : AnyI<16>;
def AnyI32 : AnyI<32>;
def AnyI64 : AnyI<64>;

// Any signless integer type irrespective of its width.
def AnySignlessInteger : Type<
  CPred<"$_self.isSignlessInteger()">, "signless integer",
        "::mlir::IntegerType">;

// Signless integer type of a specific width.
class I<int width>
    : Type<CPred<"$_self.isSignlessInteger(" # width # ")">,
                  width # "-bit signless integer", "::mlir::IntegerType">,
      BuildableType<"$_builder.getIntegerType(" # width # ")"> {
  int bitwidth = width;
}

class SignlessIntOfWidths<list<int> widths> :
    AnyTypeOf<!foreach(w, widths, I<w>),
              !interleave(widths, "/") # "-bit signless integer">;

def I1  : I<1>;
def I8  : I<8>;
def I16 : I<16>;
def I32 : I<32>;
def I64 : I<64>;
def I128 : I<128>;

// Any signed integer type irrespective of its width.
def AnySignedInteger : Type<
  CPred<"$_self.isSignedInteger()">, "signed integer">;

// Signed integer type of a specific width.
class SI<int width>
    : Type<CPred<"$_self.isSignedInteger(" # width # ")">,
                  width # "-bit signed integer", "::mlir::IntegerType">,
      BuildableType<
        "$_builder.getIntegerType(" # width # ", /*isSigned=*/true)"> {
  int bitwidth = width;
}

class SignedIntOfWidths<list<int> widths> :
    AnyTypeOf<!foreach(w, widths, SI<w>),
              !interleave(widths, "/") # "-bit signed integer">;

def SI1  : SI<1>;
def SI8  : SI<8>;
def SI16 : SI<16>;
def SI32 : SI<32>;
def SI64 : SI<64>;

// Any unsigned integer type irrespective of its width.
def AnyUnsignedInteger : Type<
  CPred<"$_self.isUnsignedInteger()">, "unsigned integer">;

// Unsigned integer type of a specific width.
class UI<int width>
    : Type<CPred<"$_self.isUnsignedInteger(" # width # ")">,
                  width # "-bit unsigned integer", "::mlir::IntegerType">,
      BuildableType<
        "$_builder.getIntegerType(" # width # ", /*isSigned=*/false)"> {
  int bitwidth = width;
}

class UnsignedIntOfWidths<list<int> widths> :
    AnyTypeOf<!foreach(w, widths, UI<w>),
              !interleave(widths, "/") # "-bit unsigned integer">;

def UI1  : UI<1>;
def UI8  : UI<8>;
def UI16 : UI<16>;
def UI32 : UI<32>;
def UI64 : UI<64>;

// Index type.
def Index : Type<CPred<"::llvm::isa<::mlir::IndexType>($_self)">, "index",
                 "::mlir::IndexType">,
            BuildableType<"$_builder.getIndexType()">;

// Any signless integer type or index type.
def AnySignlessIntegerOrIndex : Type<CPred<"$_self.isSignlessIntOrIndex()">,
                                     "signless integer or index">;

//===----------------------------------------------------------------------===//
// Floating-point types
//===----------------------------------------------------------------------===//

// Any float type irrespective of its width.
def AnyFloat : Type<CPred<"::llvm::isa<::mlir::FloatType>($_self)">, "floating-point",
                    "::mlir::FloatType">;

// Float type of a specific width.
class F<int width>
    : Type<CPred<"$_self.isF" # width # "()">,
           width # "-bit float", "::mlir::FloatType">,
      BuildableType<"$_builder.getF" # width # "Type()"> {
  int bitwidth = width;
}

class FloatOfWidths<list<int> widths> :
    AnyTypeOf<!foreach(w, widths, F<w>),
              !interleave(widths, "/") # "-bit float">;

def F16 : F<16>;
def F32 : F<32>;
def F64 : F<64>;
def F80 : F<80>;
def F128 : F<128>;

def BF16 : Type<CPred<"::llvm::isa<::mlir::BFloat16Type>($_self)">, "bfloat16 type">,
           BuildableType<"$_builder.getType<BFloat16Type>()">;
def TF32 : Type<CPred<"::llvm::isa<::mlir::FloatTF32Type>($_self)">, "tf32 type">,
           BuildableType<"$_builder.getType<FloatTF32Type>()">;
def F8E4M3FN : Type<CPred<"::llvm::isa<::mlir::Float8E4M3FNType>($_self)">, "f8E4M3FN type">,
               BuildableType<"$_builder.getType<Float8E4M3FNType>()">;
def F8E5M2 : Type<CPred<"::llvm::isa<::mlir::Float8E5M2Type>($_self)">, "f8E5M2 type">,
             BuildableType<"$_builder.getType<Float8E5M2Type>()">;
def F8E4M3 : Type<CPred<"::llvm::isa<::mlir::Float8E4M3Type>($_self)">, "f8E4M3 type">,
             BuildableType<"$_builder.getType<Float8E4M3Type>()">;
def F8E4M3FNUZ : Type<CPred<"::llvm::isa<::mlir::Float8E4M3FNUZType>($_self)">, "f8E4M3FNUZ type">,
                 BuildableType<"$_builder.getType<Float8E4M3FNUZType>()">;
def F8E5M2FNUZ : Type<CPred<"::llvm::isa<::mlir::Float8E5M2FNUZType>($_self)">, "f8E5M2FNUZ type">,
                 BuildableType<"$_builder.getType<Float8E5M2FNUZType>()">;
def F8E8M0FNU : Type<CPred<"::llvm::isa<::mlir::Float8E8M0FNUType>($_self)">, "f8E8M0FNU type">,
                BuildableType<"$_builder.getType<Float8E8M0FNUType>()">;

//===----------------------------------------------------------------------===//
// Complex types
//===----------------------------------------------------------------------===//

// Any complex type.
def AnyComplex : Type<CPred<"::llvm::isa<::mlir::ComplexType>($_self)">,
                      "complex type", "::mlir::ComplexType">;

#endif // COMMON_TYPE_CONSTRAINTS_TD
