#ifndef MLIR_NET_EXTENSIONS_TD
#define MLIR_NET_EXTENSIONS_TD

// MLIR.NET-specific extension schemas that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpExtension {
  string csharpAsmFormatCode = ?;
}

// Extension schema for Interface records.
//
// Applying this class to an upstream or local Interface record via `extends`
// lets MLIR.NET describe a C#-specific surface without hard-coding particular
// MLIR interfaces in the generator.
class MLIRNet_InterfaceExtension {
  // Optional C# interface name override. When absent, the generator derives a
  // marker-style name from cppInterfaceName.
  string csharpName = ?;
  // Optional C# XML documentation summary override for the generated interface.
  string csharpSummary = ?;
  // Optional C# XML documentation remarks/description override for the generated interface.
  string csharpDescription = ?;
  // Explicit member projections for the generated C# interface surface.
  list<MLIRNet_InterfaceMember> csharpMembers = [];
}

class MLIRNet_InterfaceMember<string upstreamNameValue, string csharpTypeValue, string csharpNameValue> {
  string upstreamName = upstreamNameValue;
  string csharpType = csharpTypeValue;
  string csharpName = csharpNameValue;
  string csharpSummary = ?;
  string csharpDescription = ?;
}

class MLIRNet_InterfaceProperty<string upstreamName, string csharpType, string csharpName>
    : MLIRNet_InterfaceMember<upstreamName, csharpType, csharpName>;

// Extension schema for concrete AttrDef/TypeDef interface-member implementations.
//
// The first implementation slice is type-focused, but the schema lives on the
// shared AttrOrTypeDef overlay so attribute support can reuse it later.
class MLIRNet_InterfaceMemberImplementation<Interface interfaceDefValue, string csharpMemberNameValue,
                                            string csharpExpressionValue> {
  Interface interfaceDef = interfaceDefValue;
  string csharpMemberName = csharpMemberNameValue;
  string csharpExpression = csharpExpressionValue;
}

class MLIRNet_InterfacePropertyImplementation<Interface interfaceDef, string csharpMemberName,
                                              string csharpExpression>
    : MLIRNet_InterfaceMemberImplementation<interfaceDef, csharpMemberName, csharpExpression>;

// Extension schema for AttrOrTypeParameter subclasses.
//
// Applying this class to a parameter class via a class-level `extends` lets
// MLIR.NET's ODS importer discover the C# type and optional parse/print
// expressions to use for that parameter in generated code.
//
// Fields:
//
//   csharpType      – Fully-qualified C# type for the property (e.g. "string",
//                     "global::MLIR.Numerics.ApInt").  Required when a
//                     custom csharpParser, csharpExtractor or csharpPrinter is
//                     provided.
//
//   csharpSyntaxType – C# concrete syntax type for the generated syntax property
//                     (e.g. "StringAttributeValueSyntax").  When set, the
//                     generated syntax class stores a strongly-typed property
//                     instead of a bare AttributeValueSyntax, allowing
//                     csharpExtractor to access typed members directly without
//                     an additional cast.  Falls back to "AttributeValueSyntax"
//                     when absent.
//
//   csharpSyntaxShape – How the generated syntax property participates in syntax
//                     rewriting. Accepted values are Token, OptionalToken, RawText,
//                     SyntaxNode, OptionalSyntaxNode, DelimitedList, DelimitedTokenList,
//                     SeparatedList, SeparatedTokenList, TokenList, RawTextList,
//                     SyntaxList, and PlainValue. When absent on a typed
//                     csharpSyntaxType, MLIR.NET treats the value as PlainValue.
//
//   csharpParser    – C# expression that parses one parameter value from text
//                     and returns a `ParseResult<AttributeValueSyntax>`.  Use
//                     `$_parser` to refer to the `AttributeParsingContext` local
//                     variable.  When absent the generator calls
//                     `$_parser.TryParseAttributeValueSyntax(…)` with the stop
//                     tokens inferred from the surrounding assembly format.
//
//   csharpExtractor – C# expression that converts an `AttributeValueSyntax` node
//                     into the typed C# value.  Use `$_syntax` to refer to the
//                     per-parameter syntax property from the structured syntax
//                     class (typed as `csharpSyntaxType` when that field is set).
//                     The expression must return `csharpType`.  When absent the
//                     generator falls back to passing the syntax node directly
//                     (only valid when `csharpType` is `AttributeValueSyntax`).
//
//   csharpDefault   – C# expression for the default/fallback value of `csharpType`
//                     when no syntax is available.  When absent the generator uses
//                     a raw empty syntax node as the default, which is only correct
//                     when `csharpType` is `AttributeValueSyntax`.
//
//   csharpPrinter   – C# expression that converts a typed parameter value into an
//                     `AttributeValueSyntax`.  Use `$_self` to refer to the typed
//                     property value.  When absent, the generator uses the syntax
//                     node stored in the structured syntax class directly (only
//                     valid when `csharpType` is `AttributeValueSyntax`).
//
// The upstream parameter classes (StringRefParameter, APIntParameter,
// APFloatParameter) are mapped via extends in AttrTypeBaseExtensions.td.
//
// Example for a user-defined parameter class:
//   class MyIntParam<string desc> : AttrOrTypeParameter<"MyIntType", desc>;
//   extends MyIntParam : MLIRNet_AttrOrTypeParameterExtension {
//     let csharpType = "global::MLIR.Numerics.ApInt";
//     let csharpSyntaxType = "IntegerAttributeValueSyntax";
//     let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";
//     let csharpExtractor = "$_syntax.Value";
//     let csharpDefault = "global::MLIR.Numerics.ApInt.Zero(64)";
//     let csharpPrinter = "new IntegerAttributeValueSyntax(TokenFactory.Integer($_self.ToStringSigned()), $_self)";
//   }
// Extension schema for AttrDef and TypeDef records.
//
// Applying this class to an AttrDef or TypeDef record via a record-level `extends` lets
// MLIR.NET's ODS importer use a declarative C# parameter list instead of inferring C#
// types from the upstream `parameters` DAG.
//
// Fields:
//
//   csharpParameters – A DAG mirroring the `parameters` field but interpreted for C#.
//                      Each argument name ($name) must match a name in `parameters`.
//
//                      Two forms are accepted per argument:
//
//                        "CSharpType":$argName
//                          The string literal is used directly as the C# type.
//
//                        SomeParamClass<...>:$argName
//                          The argument is an AttrOrTypeParameter subclass instance;
//                          C# metadata (csharpType, csharpSyntaxType, csharpParser,
//                          csharpExtractor, csharpDefault, csharpPrinter) is resolved
//                          from the class via MLIRNet_AttrOrTypeParameterExtension
//                          annotations (including class-level extends).
//
//                      When `csharpParameters` is present it overrides the C# type
//                      inference that would otherwise be performed on the `parameters`
//                      DAG.  The upstream `parameters` field remains the source of
//                      truth for C++ semantics (cppType, storage, accessors, defaults).
//
//   csharpName       – C# expression that computes the type's canonical name.
//                      Defaults to `TypeDefinition.Name` when absent.
//
//   csharpAssemblyFormat – C# expression that instantiates an `IAttributeAssemblyFormat`
//                      or `ITypeAssemblyFormat` implementation for custom binding and
//                      printing. When absent, declarative `assemblyFormat` handles the
//                      formatter path and synthetic types fall back to their attached
//                      syntax, if any.
//
//   csharpOptionalValueAccess – How optional generated operation properties test and
//                      unwrap values. Accepted values are NullCheck and
//                      NullableValueType.
//
// Example:
//   extends MyAttr : MLIRNet_AttrOrTypeDefExtension {
//     let csharpParameters = (ins
//       StringRefParameter<"desc">:$name,
//       "System.Numerics.BigInteger":$value
//     );
//   }
class MLIRNet_AttrOrTypeDefExtension {
  // DAG of C# parameter overrides, keyed by the same names as the `parameters` dag.
  dag csharpParameters = ?;
  // C# expression for the type's canonical name.
  string csharpName = ?;
  // C# expression that instantiates the custom assembly-format helper.
  string csharpAssemblyFormat = ?;
  // Explicit implementations for generated C# interface members.
  list<MLIRNet_InterfaceMemberImplementation> csharpInterfaceImplementations = [];
}

// Extension schema for upstream Attr records.
//
// Applying this class to an Attr or DialectAttr record via a record-level `extends`
// lets MLIR.NET preserve the C#-oriented storage and accessor semantics separately
// from the upstream TableGen representation.
class MLIRNet_AttrExtension {
  // C# storage type used by the generated model.
  string csharpStorageType = ?;
  // C# return/accessor type exposed by the generated model.
  string csharpReturnType = ?;
  // C# expression that converts storage into the exposed result type.
  string csharpConvertFromStorage = ?;
  // C# expression that builds a storage value from a constant input.
  // Prefer delegating to global::MLIR.Semantics.ConstantAttributeFactory so constant
  // construction logic stays centralized and consistent across constraints.
  string csharpConstBuilderCall = ?;
  // C# expression for the default result value, if any.
  string csharpDefaultValue = ?;
  // C# value-type description associated with the attr, if any.
  string csharpValueType = ?;
  // Optional value access strategy for generated operation bindings.
  string csharpOptionalValueAccess = ?;
}

class MLIRNet_AttrOrTypeParameterExtension {
  // The C# type used for the generated typed property.
  string csharpType = ?;
  // The C# syntax type for the generated syntax property (e.g. "StringAttributeValueSyntax").
  // When set, the generated syntax class stores a strongly-typed property instead of a bare
  // AttributeValueSyntax, allowing the csharpExtractor to access typed members directly
  // without an additional cast.  Falls back to "AttributeValueSyntax" when absent.
  string csharpSyntaxType = ?;
  // Structured rewrite shape for csharpSyntaxType. If omitted for a typed syntax property,
  // MLIR.NET treats the value as plain rather than guessing from the type name.
  string csharpSyntaxShape = ?;
  // Custom C# parser expression; `$_parser` refers to the AttributeParsingContext.
  // Must return `ParseResult<AttributeValueSyntax>`.
  string csharpParser = ?;
  // Custom C# extractor expression; `$_syntax` refers to the AttributeValueSyntax
  // parameter node from the structured syntax class. Must return `csharpType`.
  string csharpExtractor = ?;
  // C# expression for the default value of `csharpType` when no syntax is available.
  string csharpDefault = ?;
  // Custom C# printer expression; `$_self` refers to the typed property value.
  // Must return an `AttributeValueSyntax`.
  string csharpPrinter = ?;
}

#endif // MLIR_NET_EXTENSIONS_TD
