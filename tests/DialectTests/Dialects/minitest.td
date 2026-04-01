class MiniTest_Op<string mnemonic, list<Trait> traits = []> :
    Op<MiniTest_Dialect, mnemonic, traits>;

def MiniTest_Dialect : Dialect {
  let name = "minitest";
  let cppNamespace = "::mlir::minitest";
  let summary = "Dialect used by integration tests to cover a variety of assembly format features";
};

// Tests QualifiedDirectiveChunk.
// qualified(type(...)) has no effect on parsing; a TypeSyntax field is emitted just like a
// plain type(...) directive.
def MiniTest_CastOp : MiniTest_Op<"cast", []> {
  let summary = "qualified-type cast";

  let arguments = (ins I32:$input);
  let results = (outs I32:$result);

  let assemblyFormat = "$input attr-dict `:` qualified(type($result))";
};

// Tests OptionalGroup with a punctuation guard.
// The comma anchors the optional second operand: parsing `%lhs, %rhs : type` produces both
// operands; parsing `%lhs : type` leaves the optional fields null.
// Note: both arguments are declared in the ODS definition; the optionality is enforced only
// at the assembly level (the generated body syntax fields for commaToken and rhs are nullable).
// When rhs is absent in the custom format the binder receives a default token for that operand.
def MiniTest_BinaryOp : MiniTest_Op<"binary", []> {
  let summary = "binary op with optional second operand";

  let arguments = (ins I32:$lhs, I32:$rhs);
  let results = (outs I32:$result);

  let assemblyFormat = "$lhs (`,` $rhs^)? attr-dict `:` type($result)";
};

// Tests OilistDirectiveChunk.
// The two keyword-prefixed attribute clauses may appear in any order, zero or more times.
// Note: both attributes are declared in the ODS definition; each is individually optional in
// the oilist format.  When a clause is absent its body-syntax field is null and the binder
// receives a null-forgiving call, so callers that invoke Bind should supply both clauses.
def MiniTest_ConfigOp : MiniTest_Op<"config", []> {
  let summary = "op with order-independent keyword-prefixed attribute clauses";

  let arguments = (ins I32Attr:$stride, I32Attr:$padding);

  let assemblyFormat = "oilist(`stride` $stride | `padding` $padding) attr-dict";
};
