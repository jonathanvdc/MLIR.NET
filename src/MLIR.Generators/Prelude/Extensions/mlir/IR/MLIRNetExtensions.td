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
//   csharpType    – Fully-qualified C# type for the property (e.g. "string",
//                   "global::System.Numerics.BigInteger").  Required when a
//                   custom csharpParser or csharpPrinter is provided.
//
//   csharpParser  – C# expression that parses one parameter value and returns a
//                   `ParseResult<AttributeValueSyntax>`.  Use `$_parser` to
//                   refer to the `AttributeParsingContext` local variable.
//                   When absent, the generator calls
//                   `$_parser.TryParseAttributeValueSyntax(…)` with the stop
//                   tokens inferred from the surrounding assembly format.
//
//   csharpPrinter – C# expression that converts a typed parameter value into an
//                   `AttributeValueSyntax`.  Use `$_self` to refer to the typed
//                   property value.  When absent, the generator synthesizes a
//                   printer from `csharpType` (e.g. `string` → wraps in
//                   `StringAttributeValueSyntax`).
//
// The upstream parameter classes (StringRefParameter, APIntParameter,
// APFloatParameter) are mapped via extends in AttrTypeBaseExtensions.td.
//
// Example for a user-defined parameter class:
//   class MyIntParam<string desc> : AttrOrTypeParameter<"MyIntType", desc>;
//   extends MyIntParam : MLIRNet_AttrOrTypeParameterExtension {
//     let csharpType = "global::MyNamespace.MyInt";
//     let csharpParser = "$_parser.TryParseAttributeValueSyntax()";
//     let csharpPrinter = "new RawAttributeValueSyntax(new RawSyntaxText($_self.ToString()))";
//   }
class MLIRNet_AttrOrTypeParameterExtension {
  // The C# type used for the generated typed property.
  string csharpType = ?;
  // Custom C# parser expression; `$_parser` refers to the AttributeParsingContext.
  // Must return `ParseResult<AttributeValueSyntax>`.
  string csharpParser = ?;
  // Custom C# printer expression; `$_self` refers to the typed property value.
  // Must return an `AttributeValueSyntax`.
  string csharpPrinter = ?;
}

#endif // MLIR_NET_EXTENSIONS_TD
