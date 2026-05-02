include "mlir/Extensions/IR/CSharpExtensions.td"

#ifndef ATTR_TYPE_BASE_EXTENSIONS_TD
#define ATTR_TYPE_BASE_EXTENSIONS_TD

// Map well-known upstream AttrOrTypeParameter subclasses to their C# equivalents.
// These classes are defined in the vendored upstream AttrTypeBase.td and cannot be
// modified directly; class-level extends lets us attach the metadata here instead.

extends StringRefParameter : CSharpStringLiteralParameter;

extends APIntParameter : CSharpIntegerLiteralParameter;

extends APFloatParameter : CSharpFloatLiteralParameter;

extends AttributeSelfTypeParameter : CSharpAttributeSelfTypeParameter;

#endif // ATTR_TYPE_BASE_EXTENSIONS_TD
