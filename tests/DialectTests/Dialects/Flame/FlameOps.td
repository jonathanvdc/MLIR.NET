#ifndef FLAME_OPS
#define FLAME_OPS

include "mlir/IR/OpBase.td"
include "mlir/Interfaces/CallInterfaces.td"
include "mlir/Interfaces/ControlFlowInterfaces.td"
include "mlir/Interfaces/SideEffectInterfaces.td"

include "FlameDialect.td"
include "FlameTypes.td"
include "FlameAttrs.td"

class Flame_Op<string mnemonic, list<Trait> traits = []>
    : Op<Flame_Dialect, mnemonic, traits>;

class Flame_TerminatorOp<string mnemonic, list<Trait> traits = []>
    : Op<Flame_Dialect, mnemonic, !listconcat(traits, [Terminator])>;

def Flame_ConstOp : Flame_Op<"const", [Pure]> {
  let summary = "Constant";
  let description = [{
    Materializes a constant value. Prefer builtin attributes directly here
    instead of wrapping them in Flame-specific attribute shells unless the
    wrapper carries real Flame semantics.
  }];
  let arguments = (ins
    AnyAttr:$value
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$value attr-dict `:` type($result)";
}

def Flame_AllocaOp : Flame_Op<"alloca", [MemoryEffects<[MemAlloc]>]> {
  let summary = "Allocate one object";
  let arguments = (ins
    TypeAttr:$elementType
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$elementType attr-dict `:` type($result)";
}

def Flame_AllocaArrayOp : Flame_Op<"alloca_array", [MemoryEffects<[MemAlloc]>]> {
  let summary = "Allocate an array";
  let arguments = (ins
    TypeAttr:$elementType,
    AnyType:$count
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$elementType `,` $count attr-dict `:` type($count) `->` type($result)";
}

def Flame_BoxOp : Flame_Op<"box", [Pure]> {
  let summary = "Box a value";
  let arguments = (ins
    AnyType:$input,
    TypeAttr:$elementType
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$input `:` type($input) `as` $elementType attr-dict `->` type($result)";
}

def Flame_CopyOp : Flame_Op<"copy", [Pure]> {
  let summary = "Copy a value";
  let arguments = (ins
    AnyType:$input
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$input attr-dict `:` type($input) `->` type($result)";
}

def Flame_LoadOp : Flame_Op<"load",
    [MemoryEffects<[MemRead]>]> {
  let summary = "Load through a pointer";
  let arguments = (ins
    AnyType:$ptr,
    UnitAttr:$volatile,
    OptionalAttr<I64Attr>:$alignment
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$ptr (`volatile` $volatile^)? (`,` `align` `=` $alignment^)? attr-dict `:` type($ptr) `->` type($result)";
}

def Flame_StoreOp : Flame_Op<"store",
    [MemoryEffects<[MemWrite]>]> {
  let summary = "Store through a pointer";
  let arguments = (ins
    AnyType:$value,
    AnyType:$ptr,
    UnitAttr:$volatile,
    OptionalAttr<I64Attr>:$alignment
  );
  let assemblyFormat = "$value `,` $ptr (`volatile` $volatile^)? (`,` `align` `=` $alignment^)? attr-dict `:` type($value) `,` type($ptr)";
}

def Flame_GetFieldPointerOp : Flame_Op<"get_field_pointer", [Pure]> {
  let summary = "Get pointer to instance field";
  let arguments = (ins
    AnyType:$base,
    FlatSymbolRefAttr:$field
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$base `,` $field attr-dict `:` type($base) `->` type($result)";
}

def Flame_GetStaticFieldPointerOp : Flame_Op<"get_static_field_pointer", [Pure]> {
  let summary = "Get pointer to static field";
  let arguments = (ins
    FlatSymbolRefAttr:$field
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$field attr-dict `:` type($result)";
}

def Flame_LoadFieldOp : Flame_Op<"load_field", [Pure]> {
  let summary = "Fused instance-field load";
  let arguments = (ins
    AnyType:$base,
    FlatSymbolRefAttr:$field
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$base `,` $field attr-dict `:` type($base) `->` type($result)";
}

def Flame_StoreFieldOp : Flame_Op<"store_field"> {
  let summary = "Fused instance-field store";
  let arguments = (ins
    AnyType:$base,
    AnyType:$value,
    FlatSymbolRefAttr:$field
  );
  let assemblyFormat = "$base `,` $value `,` $field attr-dict `:` type($base) `,` type($value)";
}

def Flame_CallOp : Flame_Op<"call", [CallOpInterface]> {
  let summary = "Direct call";
  let description = [{
    The encoder models instructions as a prototype applied to SSA arguments.
    Direct calls therefore carry the callee as an embedded symbol reference,
    with the actual instruction operands represented as SSA operands.
  }];
  let arguments = (ins
    FlatSymbolRefAttr:$callee,
    Variadic<AnyType>:$args
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$callee `(` $args `)` attr-dict (`:` functional-type(operands, results))?";
}

def Flame_ConstrainedCallOp : Flame_Op<"constrained_call", [CallOpInterface]> {
  let summary = "Constrained direct call";
  let arguments = (ins
    FlatSymbolRefAttr:$callee,
    Variadic<AnyType>:$args,
    DefaultValuedAttr<StrAttr, "\"static\"">:$lookup
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$callee `(` $args `)` (`lookup` `=` $lookup^)? attr-dict (`:` functional-type(operands, results))?";
}

def Flame_IndirectCallOp : Flame_Op<"indirect_call", [CallOpInterface]> {
  let summary = "Indirect call";
  let arguments = (ins
    AnyType:$callee,
    Variadic<AnyType>:$args
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$callee `(` $args `)` attr-dict `:` functional-type(operands, results)";
}

def Flame_IntrinsicOp : Flame_Op<"intrinsic", [Pure]> {
  let summary = "Intrinsic invocation";
  let arguments = (ins
    StrAttr:$name,
    Variadic<AnyType>:$args
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$name `(` $args `)` attr-dict (`:` functional-type(operands, results))?";
}

def Flame_NewObjectOp : Flame_Op<"new_object"> {
  let summary = "Object construction";
  let arguments = (ins
    FlatSymbolRefAttr:$constructor,
    Variadic<AnyType>:$args
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$constructor `(` $args `)` attr-dict (`:` functional-type(operands, results))?";
}

def Flame_NewDelegateOp : Flame_Op<"new_delegate", [Pure]> {
  let summary = "Create delegate";
  let arguments = (ins
    Optional<AnyType>:$receiver,
    FlatSymbolRefAttr:$callee,
    I1Attr:$hasThisArgument,
    DefaultValuedAttr<StrAttr, "\"static\"">:$lookup
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "(`receiver` $receiver^ `,`)? $callee `,` $hasThisArgument (`lookup` `=` $lookup^)? attr-dict `:` type($result)";
}

def Flame_DynamicCastOp : Flame_Op<"dynamic_cast", [Pure]> {
  let summary = "Dynamic cast";
  let arguments = (ins
    AnyType:$input,
    TypeAttr:$targetType
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$input `to` $targetType attr-dict `:` type($input) `->` type($result)";
}

def Flame_ReinterpretCastOp : Flame_Op<"reinterpret_cast", [Pure]> {
  let summary = "Reinterpret cast";
  let arguments = (ins
    AnyType:$input,
    TypeAttr:$targetType
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$input `to` $targetType attr-dict `:` type($input) `->` type($result)";
}

def Flame_UnboxOp : Flame_Op<"unbox", [Pure]> {
  let summary = "Unbox a value";
  let arguments = (ins
    AnyType:$input,
    TypeAttr:$targetType
  );
  let results = (outs
    AnyType:$result
  );
  let assemblyFormat = "$input `to` $targetType attr-dict `:` type($input) `->` type($result)";
}

def Flame_BrOp : Flame_TerminatorOp<"br", [BranchOpInterface]> {
  let summary = "Unconditional branch";
  let arguments = (ins
    Variadic<AnyType>:$destOperands
  );
  let successors = (successor AnySuccessor:$dest);
  let assemblyFormat = "$dest `(` $destOperands `)` attr-dict";
}

def Flame_ReturnOp : Flame_TerminatorOp<"return", [ReturnLike, Pure]> {
  let summary = "Return from the current method";
  let arguments = (ins
    Variadic<AnyType>:$operands
  );
  let assemblyFormat = "$operands attr-dict";
}

def Flame_UnreachableOp : Flame_TerminatorOp<"unreachable", [Pure]> {
  let summary = "Unreachable terminator";
  let assemblyFormat = "attr-dict";
}

def Flame_SwitchOp : Flame_TerminatorOp<"switch", [BranchOpInterface, AttrSizedOperandSegments]> {
  let summary = "Switch on an SSA value";
  let description = [{
    A switch terminator with one default destination and zero or more case
    destinations.

    Each case conceptually consists of three pieces of information:
      - a case constant,
      - a successor block,
      - the block arguments forwarded to that successor.

    ODS does not model this nested structure directly, so the representation is
    flattened:
      - `caseValues` stores one constant per case,
      - `caseDests` stores one successor per case,
      - `caseOperands` stores the concatenation of all forwarded case operands,
      - `caseOperandSegments` stores how many operands belong to each case.

    The default destination is modeled separately as `defaultDest` with its own
    forwarded operands in `defaultOperands`.

    This op should eventually use a custom parser/printer so the textual syntax
    can present cases as structured entries instead of exposing the flattened
    storage directly.
  }];
  let arguments = (ins
    AnyType:$value,
    Variadic<AnyType>:$defaultOperands,
    Variadic<AnyType>:$caseOperands,
    ArrayAttr:$caseValues,
    DenseI32ArrayAttr:$caseOperandSegments
  );
  let successors = (successor
    AnySuccessor:$defaultDest,
    VariadicSuccessor<AnySuccessor>:$caseDests
  );
  let hasCustomAssemblyFormat = 1;
  let hasVerifier = 1;
}

def Flame_InvokeOp : Flame_TerminatorOp<"invoke", [CallOpInterface, BranchOpInterface]> {
  let summary = "Exceptional call terminator";
  let description = [{
    The source encoder models TryFlow as a flow construct consisting of an
    instruction plus a success and exception branch. In MLIR terms that is much
    closer to an invoke-like terminator than to an ordinary value-producing op.
  }];
  let arguments = (ins
    FlatSymbolRefAttr:$callee,
    Variadic<AnyType>:$args,
    Variadic<AnyType>:$normalOperands,
    Variadic<AnyType>:$exceptionOperands
  );
  let successors = (successor
    AnySuccessor:$normalDest,
    AnySuccessor:$exceptionDest
  );
  let assemblyFormat = "$callee `(` $args `)` `to` $normalDest `(` $normalOperands `)` `except` $exceptionDest `(` $exceptionOperands `)` attr-dict `:` type($args)";
}

#endif // FLAME_OPS