include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef ATTR_TYPE_BASE_EXTENSIONS_TD
#define ATTR_TYPE_BASE_EXTENSIONS_TD

// Map well-known upstream AttrOrTypeParameter subclasses to their C# equivalents.
// These classes are defined in the vendored upstream AttrTypeBase.td and cannot be
// modified directly; class-level extends lets us attach the metadata here instead.

extends StringRefParameter : MLIRNet_AttrOrTypeParameterExtension {
  // StringRef exposes as a reference into std::string storage. In C# this is just string.
  let csharpType = "string";
  // The parser returns a StringAttributeValueSyntax, so the property can be strongly typed.
  let csharpSyntaxType = "StringAttributeValueSyntax";
  // Parse a quoted string literal and return it as StringAttributeValueSyntax so the
  // extractor can recover the unescaped value without reparsing raw text.
  let csharpParser = "$_parser.TryParseStringLiteralSyntax()";
  // The property is already StringAttributeValueSyntax; directly access its Value.
  let csharpExtractor = "$_syntax.Value";
  // Default to an empty string when no syntax is available.
  let csharpDefault = "string.Empty";
  // Build a quoted StringAttributeValueSyntax from the typed string value.
  let csharpPrinter = "new StringAttributeValueSyntax(new SyntaxToken(StringLiteralAttributeAssemblyFormat.Quote($_self)), $_self)";
}

extends APIntParameter : MLIRNet_AttrOrTypeParameterExtension {
  // APInt has arbitrary precision; resolve to a 64-bit signless integer (MLIR's i64 default)
  // at parse time, matching MLIR's integer-literal default type resolution.
  let csharpType = "global::MLIR.Numerics.ApInt";
  // The parser returns an IntegerAttributeValueSyntax, so the property can be strongly typed.
  let csharpSyntaxType = "IntegerAttributeValueSyntax";
  // Parse a signed integer literal and return it as IntegerAttributeValueSyntax.
  let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";
  // Extract as a 64-bit ApInt by parsing the literal token text with signed interpretation.
  // This resolves the integer at parse time rather than deferring it to bind time.
  let csharpExtractor = "global::MLIR.Numerics.ApInt.Parse(64, $_syntax.LiteralToken.Text, 10, true)";
  // Default to zero at 64-bit width when no syntax is available.
  let csharpDefault = "global::MLIR.Numerics.ApInt.Zero(64)";
  // Build an IntegerAttributeValueSyntax from the ApInt's signed decimal representation.
  let csharpPrinter = "new IntegerAttributeValueSyntax(new SyntaxToken($_self.ToStringSigned()), $_self.ToBigIntegerSigned())";
}

extends APFloatParameter : MLIRNet_AttrOrTypeParameterExtension {
  // ApFloat with IEEEDouble (f64) semantics — MLIR's default for float literals without
  // an explicit type annotation.
  let csharpType = "global::MLIR.Numerics.ApFloat";
  // The parser returns a FloatingPointAttributeValueSyntax, so the property can be strongly typed.
  let csharpSyntaxType = "FloatingPointAttributeValueSyntax";
  // Parse a decimal floating-point literal and return it as FloatingPointAttributeValueSyntax.
  let csharpParser = "$_parser.TryParseFloatingPointLiteralSyntax()";
  // Extract as ApFloat with IEEEDouble semantics by parsing the literal text at parse time.
  // This resolves the float at parse time rather than deferring it to bind time.
  let csharpExtractor = "global::MLIR.Numerics.ApFloat.Parse(global::MLIR.Numerics.FloatSemantics.IEEEDouble, $_syntax.LiteralText)";
  // Default to zero with IEEEDouble semantics when no syntax is available.
  let csharpDefault = "global::MLIR.Numerics.ApFloat.Zero(global::MLIR.Numerics.FloatSemantics.IEEEDouble)";
  // Build a FloatingPointAttributeValueSyntax from the ApFloat using MLIR-parseable literal text.
  let csharpPrinter = "new FloatingPointAttributeValueSyntax(new global::MLIR.Syntax.RawSyntaxText($_self.ToLiteralText()), $_self.ToLiteralText())";
}

#endif // ATTR_TYPE_BASE_EXTENSIONS_TD
