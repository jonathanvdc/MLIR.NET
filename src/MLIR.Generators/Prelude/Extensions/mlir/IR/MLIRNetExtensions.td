// MLIR.NET-specific extension records that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpExtension<string op> {
  string opName = op;
  string csharpAsmFormatCode = ?;
}
