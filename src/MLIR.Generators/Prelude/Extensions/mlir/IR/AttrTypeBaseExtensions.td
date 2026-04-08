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
  // APInt has arbitrary precision; BigInteger is the lossless C# equivalent.
  let csharpType = "global::System.Numerics.BigInteger";
  // The parser returns an IntegerAttributeValueSyntax, so the property can be strongly typed.
  let csharpSyntaxType = "IntegerAttributeValueSyntax";
  // Parse a signed integer literal and return it as IntegerAttributeValueSyntax.
  let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";
  // The property is already IntegerAttributeValueSyntax; directly access its Value.
  let csharpExtractor = "$_syntax.Value";
  // Default to zero when no syntax is available.
  let csharpDefault = "default(global::System.Numerics.BigInteger)";
  // Build an IntegerAttributeValueSyntax from the typed BigInteger value.
  let csharpPrinter = "new IntegerAttributeValueSyntax(new SyntaxToken($_self.ToString(global::System.Globalization.CultureInfo.InvariantCulture)), $_self)";
}

extends APFloatParameter : MLIRNet_AttrOrTypeParameterExtension {
  // APFloat maps to double as the closest standard C# floating-point type.
  let csharpType = "double";
  // The parser returns a FloatingPointAttributeValueSyntax, so the property can be strongly typed.
  let csharpSyntaxType = "FloatingPointAttributeValueSyntax";
  // Parse a decimal floating-point literal and return it as FloatingPointAttributeValueSyntax.
  let csharpParser = "$_parser.TryParseFloatingPointLiteralSyntax()";
  // The property is already FloatingPointAttributeValueSyntax; access LiteralText and parse.
  let csharpExtractor = "double.TryParse($_syntax.LiteralText, global::System.Globalization.NumberStyles.Float, global::System.Globalization.CultureInfo.InvariantCulture, out var fpVal) ? fpVal : default(double)";
  // Default to zero when no syntax is available.
  let csharpDefault = "default(double)";
  // Build a FloatingPointAttributeValueSyntax from the typed double value.
  let csharpPrinter = "new FloatingPointAttributeValueSyntax(new global::MLIR.Syntax.RawSyntaxText($_self.ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture)), $_self.ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture))";
}

#endif // ATTR_TYPE_BASE_EXTENSIONS_TD
