#ifndef MLIR_NET_EXTENSIONS_TD
#define MLIR_NET_EXTENSIONS_TD

// MLIR.NET-specific extension records that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpExtension<string op> {
  string opName = op;
  string csharpAsmFormatCode = ?;
}

#endif // MLIR_NET_EXTENSIONS_TD
