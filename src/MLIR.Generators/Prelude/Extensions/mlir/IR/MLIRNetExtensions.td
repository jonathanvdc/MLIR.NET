#ifndef MLIR_NET_EXTENSIONS_TD
#define MLIR_NET_EXTENSIONS_TD

// MLIR.NET-specific extension schemas that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpExtension {
  string csharpAsmFormatCode = ?;
}

// Extension schema for AttrOrTypeParameter subclasses.
// Applying this class to a parameter class via a class-level extends lets
// MLIR.NET's ODS importer discover the C# type to use for that parameter in
// generated code. The 'csharpType' field accepts any fully-qualified C# type
// expression (e.g., "string", "global::System.Numerics.BigInteger").
//
// The upstream parameter classes (StringRefParameter, APIntParameter,
// APFloatParameter) are mapped via extends in AttrTypeBaseExtensions.td.
//
// Example for a user-defined parameter class:
//   class MyIntParam<string desc> : AttrOrTypeParameter<"MyIntType", desc>;
//   extends MyIntParam : MLIRNet_AttrOrTypeParameterExtension {
//     let csharpType = "global::MyNamespace.MyInt";
//   }
class MLIRNet_AttrOrTypeParameterExtension {
  string csharpType = ?;
}

#endif // MLIR_NET_EXTENSIONS_TD
