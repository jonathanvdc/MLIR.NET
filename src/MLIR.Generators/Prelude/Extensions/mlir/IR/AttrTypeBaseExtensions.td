include "mlir/Extensions/IR/MLIRNetExtensions.td"

#ifndef ATTR_TYPE_BASE_EXTENSIONS_TD
#define ATTR_TYPE_BASE_EXTENSIONS_TD

// Map well-known upstream AttrOrTypeParameter subclasses to their C# equivalents.
// These classes are defined in the vendored upstream AttrTypeBase.td and cannot be
// modified directly; class-level extends lets us attach the metadata here instead.

extends StringRefParameter : MLIRNet_AttrOrTypeParameterExtension {
  // StringRef exposes as a reference into std::string storage. In C# this is just string.
  let csharpType = "string";
}

extends APIntParameter : MLIRNet_AttrOrTypeParameterExtension {
  // APInt has arbitrary precision; BigInteger is the lossless C# equivalent.
  let csharpType = "global::System.Numerics.BigInteger";
}

extends APFloatParameter : MLIRNet_AttrOrTypeParameterExtension {
  // APFloat maps to double as the closest standard C# floating-point type.
  let csharpType = "double";
}

#endif // ATTR_TYPE_BASE_EXTENSIONS_TD
