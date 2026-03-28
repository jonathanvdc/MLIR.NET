# MLIR.NET

MLIR.NET is a C#/.NET project for working with [MLIR](https://mlir.llvm.org/) text and dialect descriptions.

At a high level, this repository aims to provide two things:

- a runtime library for parsing, printing, and binding MLIR syntax in C#
- a generation pipeline that turns MLIR ODS/TableGen dialect descriptions into typed C# APIs

The project is still early, but it already supports a useful end-to-end flow:

- parse and print generic MLIR syntax while preserving tokens and trivia
- bind parsed syntax into typed semantic nodes
- import a real ODS-style subset of TableGen
- generate dialect registration and typed operation classes with a Roslyn source generator

## What This Project Does

MLIR has two complementary sides:

1. IR text and syntax
2. dialect definitions, usually written in TableGen/ODS

This repository covers both.

On the syntax side, the runtime library can parse MLIR text into a concrete syntax tree, print it back out, and bind it into a semantic model that can be traversed and extended by dialects.

On the dialect side, the generator pipeline reads ODS-style TableGen and produces typed C# code that plugs into normal SDK-style .NET projects through Roslyn source generation.

That makes it possible to move from:

- `.td` dialect description

to:

- generated `Dialect` registration
- generated typed operation classes
- generated C# namespaces derived from `cppNamespace`

without introducing a manual code generation step into the consumer project.

## Current Scope

The current implementation supports a real ODS-style subset rather than a repo-local toy format.

That includes shapes such as:

- `def X : Dialect`
- `class Y_Op<string mnemonic, list<Trait> traits = []> : Op<DialectDef, mnemonic, traits>;`
- `let arguments = (ins ...)`
- `let results = (outs ...)`
- `let assemblyFormat = "..."`
- dialect metadata such as `cppNamespace`, `summary`, `description`, and `hasConstantMaterializer`

The project does **not** yet implement the full upstream MLIR ODS surface. In particular, support for the entire LLVM/MLIR TableGen ecosystem, includes, and complete declarative assembly-format semantics is still incomplete.

## Quick Example

Parsing and printing MLIR text:

```csharp
using MLIR;

var document = Document.Parse("%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32");
var roundTripped = document.ToText();
```

Using a generated dialect in a normal C# project:

```csharp
using MLIR.Dialects;
using MLIR.Miniarith;

Dialect dialect = MiniarithDialectRegistration.Create();
Type addType = typeof(MiniArith_AddIOp);
Type constantType = typeof(MiniArith_ConstantOp);
```

That generated API comes from an ODS-style `.td` file such as:

```tablegen
class MiniArith_Op<string mnemonic, list<Trait> traits = []> :
    Op<MiniArith_Dialect, mnemonic, traits>;

def MiniArith_Dialect : Dialect {
  let name = "miniarith";
  let cppNamespace = "::mlir::miniarith";
}

def MiniArith_AddIOp : MiniArith_Op<"addi", [Pure, Commutative]> {
  let arguments = (ins I32:$lhs, I32:$rhs);
  let results = (outs I32:$result);
  let assemblyFormat = "$lhs `,` $rhs attr-dict `:` type($result)";
}
```

## How It Is Organized

The repository is split into a small pipeline of projects:

- `src/TableGen`
  Parses and evaluates TableGen.
- `src/MLIR.ODS`
  Imports interpreted TableGen records into an internal ODS model.
- `src/MLIR.Generators`
  Roslyn incremental source generator that turns ODS models into C#.
- `src/MLIR`
  Runtime library for MLIR CST, parsing, printing, semantics, dialect registration, and syntax transforms.

This separation is intentional. If a new feature belongs in the TableGen language layer, it should be added there first instead of being approximated later in the importer or generator.

## Generated Namespaces

Generated C# namespaces come from the dialect's `cppNamespace`.

Examples:

- `::mlir::arith` -> `MLIR.Arith`
- `::mlir::miniarith` -> `MLIR.Miniarith`
- `::mlir::foo_bar` -> `MLIR.FooBar`

The first `mlir` segment is mapped to `MLIR`, and later segments are converted to PascalCase.

## Runtime Model

The runtime keeps syntax and semantics deliberately separate.

- `Parser` parses text into a concrete syntax tree
- `Printer` prints syntax
- `Binder` binds syntax into typed semantic nodes
- `AssemblySyntaxBuilder` rewrites semantic modules into syntax, including custom assembly forms
- `GenericSyntaxBuilder` lowers custom syntax back to generic MLIR syntax

The concrete syntax tree is the source of truth for printing. Custom assembly behavior is intended to live in syntax transforms and dialect hooks rather than as printer-only special cases.

## Repository Layout

```text
src/
  MLIR/             Runtime library
  MLIR.Generators/  Roslyn source generator
  MLIR.ODS/         ODS importer/model
  TableGen/         TableGen parser/evaluator
tests/
  DialectTests/             Analyzer-backed generated-dialect integration tests
  MLIR.Generators.Tests/    Importer and source-generation tests
  MLIR.Tests/               Runtime tests
  TableGen.Tests/           TableGen language tests
samples/
  GeneratedDialectConsumer/ Sample consumer project using generated dialect code
```

## Building And Testing

Useful commands:

```bash
dotnet build MLIR.slnx
dotnet test MLIR.slnx -m:1
dotnet build samples/GeneratedDialectConsumer/GeneratedDialectConsumer.csproj
```

Targeted test suites:

```bash
dotnet test tests/TableGen.Tests/TableGen.Tests.csproj
dotnet test tests/MLIR.Generators.Tests/MLIR.Generators.Tests.csproj
dotnet test tests/DialectTests/DialectTests.csproj
dotnet test tests/MLIR.Tests/MLIR.Tests.csproj
```

If you are working on `TableGen` or `MLIR.Generators`, prefer sequential `dotnet` runs. Parallel builds/tests can cause DLL lock failures in `obj/`.

## Where To Look Next

- [samples/GeneratedDialectConsumer/Program.cs](/Users/jonathanvdc/Code/MLIR.NET/samples/GeneratedDialectConsumer/Program.cs)
  Small consumer project using generated dialect types.
- [tests/DialectTests/Dialects/arith.td](/Users/jonathanvdc/Code/MLIR.NET/tests/DialectTests/Dialects/arith.td)
  Real ODS-style dialect fixture used by the analyzer-backed integration tests.
- [AGENTS.md](/Users/jonathanvdc/Code/MLIR.NET/AGENTS.md)
  Repository-specific contributor and agent guidance.
