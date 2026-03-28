def ArithDialect {
  string DialectName = "arith";
  string DialectClassName = "ArithDialectRegistration";
};

def AddI {
  string DialectName = "arith";
  string OperationName = "arith.addi";
  string ClassName = "AddIOperation";
  list<string> Operands = ["lhs", "rhs"];
  list<string> Results = ["result"];
  list<string> Attributes = ["fastmath"];
  bit HasCustomAssemblyFormat = 1;
};

def FastMathAttr {
  string DialectName = "arith";
  string AttributeName = "fastmath";
  string ClassName = "FastMathAttributeValue";
};

def I32Type {
  string DialectName = "arith";
  string TypeName = "i32";
  string ClassName = "I32TypeReference";
};
