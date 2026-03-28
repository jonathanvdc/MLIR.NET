class Dialect {
  string name;
  string cppNamespace;
  string summary;
  string description;
  bit hasConstantMaterializer = 0;
};

class Op<string dialectName, string mnemonic> {
  string dialectName = dialectName;
  string mnemonic = mnemonic;
  string cppClassName;
  list<string> operands;
  list<string> results;
  bit hasCustomAssemblyFormat = 0;
};

class AttrDef<string dialectName, string attrName> {
  string dialectName = dialectName;
  string attrName = attrName;
  string cppClassName;
};

class TypeDef<string dialectName, string typeName> {
  string dialectName = dialectName;
  string typeName = typeName;
  string cppClassName;
};

def Arith_Dialect : Dialect {
  let name = "arith";
  let cppNamespace = "::mlir::arith";
  let summary = "Arithmetic dialect";
  let description = [{
    This dialect defines basic integer and floating point arithmetic ops.
  }];
  let hasConstantMaterializer = 1;
};

def AddIOp : Op<"arith", "addi"> {
  let cppClassName = "AddIOperation";
  let operands = ["lhs", "rhs"];
  let results = ["result"];
  let hasCustomAssemblyFormat = 1;
};

def FastMathAttr : AttrDef<"arith", "fastmath"> {
  let cppClassName = "FastMathAttributeValue";
};

def I32Type : TypeDef<"arith", "i32"> {
  let cppClassName = "I32TypeReference";
};
