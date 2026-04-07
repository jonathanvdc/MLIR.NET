#ifndef MLIR_NET_EXTENSIONS_TD
#define MLIR_NET_EXTENSIONS_TD

// MLIR.NET-specific extension schemas that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpExtension {
  string csharpAsmFormatCode = ?;
}

// Extension schema for AttrOrTypeParameter subclasses.
//
// Applying this class to a parameter class via a class-level `extends` lets
// MLIR.NET's ODS importer discover the C# type and optional parse/print
// expressions to use for that parameter in generated code.
//
// Fields:
//
//   csharpType      – Fully-qualified C# type for the property (e.g. "string",
//                     "global::System.Numerics.BigInteger").  Required when a
//                     custom csharpParser, csharpExtractor or csharpPrinter is
//                     provided.
//
//   csharpParser    – C# expression that parses one parameter value from text
//                     and returns a `ParseResult<AttributeValueSyntax>`.  Use
//                     `$_parser` to refer to the `AttributeParsingContext` local
//                     variable.  When absent the generator calls
//                     `$_parser.TryParseAttributeValueSyntax(…)` with the stop
//                     tokens inferred from the surrounding assembly format.
//
//   csharpExtractor – C# expression that converts a `AttributeValueSyntax` node
//                     into the typed C# value.  Use `$_syntax` to refer to the
//                     `AttributeValueSyntax` variable that holds the per-parameter
//                     syntax node from the structured syntax class.  The expression
//                     must return `csharpType`.  When absent the generator falls
//                     back to passing the syntax node directly (only valid when
//                     `csharpType` is `AttributeValueSyntax`).
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
//     let csharpType = "global::MyNamespace.MyInt";
//     let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";
//     let csharpExtractor = "$_syntax is IntegerAttributeValueSyntax iv ? (global::MyNamespace.MyInt)iv.Value : default";
//     let csharpDefault = "default(global::MyNamespace.MyInt)";
//     let csharpPrinter = "new RawAttributeValueSyntax(new RawSyntaxText($_self.ToString()))";
//   }
class MLIRNet_AttrOrTypeParameterExtension {
  // The C# type used for the generated typed property.
  string csharpType = ?;
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
