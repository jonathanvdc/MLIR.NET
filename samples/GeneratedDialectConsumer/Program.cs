using MLIR.Dialects;
using MLIR.Arith;

Dialect dialect = ArithDialectRegistration.Create();
System.Type operationType = typeof(AddIOperation);
System.Type attributeType = typeof(FastMathAttributeValue);
System.Type typeReferenceType = typeof(I32TypeReference);

System.Console.WriteLine($"{dialect.Name}: {operationType.Name}, {attributeType.Name}, {typeReferenceType.Name}");
