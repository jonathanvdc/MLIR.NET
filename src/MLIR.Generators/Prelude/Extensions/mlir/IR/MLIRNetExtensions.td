// MLIR.NET-specific extension records that overlay upstream ODS definitions
// without editing the vendored upstream files directly.

class MLIRNet_OpAsmExtension<string op, string asmStrategy> {
  string opName = op;
  string strategy = asmStrategy;
}
