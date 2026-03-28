using MLIR.Dialects;
using MLIR.Miniarith;

Dialect dialect = MiniarithDialectRegistration.Create();
System.Type addType = typeof(MiniArith_AddIOp);
System.Type constantType = typeof(MiniArith_ConstantOp);

System.Console.WriteLine($"{dialect.Name}: {addType.Name}, {constantType.Name}");
