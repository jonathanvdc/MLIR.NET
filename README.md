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

- `include "mlir/IR/OpBase.td"` with `def X : Dialect`
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
include "mlir/IR/OpBase.td"

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
- `ConcreteSyntaxBuilder` rewrites semantic modules into syntax, including custom assembly forms, and it can be configured to prefer custom assembly or the generic format while optionally rebuilding existing CST nodes to match the chosen preference
- `GenericSyntaxBuilder` lowers custom syntax back to generic MLIR syntax

The concrete syntax tree is the source of truth for printing. Custom assembly behavior is intended to live in syntax transforms and dialect hooks rather than as printer-only special cases.

## Repository Layout

```text
src/
  MLIR/             Runtime library
  MLIR.Generators/  Roslyn source generator
  MLIR.ODS/         ODS importer/model
  TableGen/         TableGen parser/evaluator
tools/
  TableGenDebug/    Debug utility for evaluating a TableGen file and printing records
  TdToCSharp/       Utility for compiling .td files into generated C# dialect sources
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

## TableGen Debugging Utility

Use `tools/TableGenDebug` when you want to inspect the records produced by a TableGen file after evaluation.

The tool loads the file, resolves includes from the embedded MLIR prelude shipped with `MLIR.Generators`, evaluates the document, and prints the resulting records.

```bash
dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- <file.td> [record-pattern]
```

The optional `record-pattern` argument is a glob-style filter over record names:

- `*` matches any record name
- `?` matches a single character

Examples:

```bash
dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- samples/GeneratedDialectConsumer/Dialects/arith.td
dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- samples/GeneratedDialectConsumer/Dialects/arith.td 'Arith_*'
```

## TableGen-To-C# Utility

Use `tools/TdToCSharp` when you want to inspect the generated C# for one or more standalone `.td` files without going through an SDK-style consumer project build.

The tool reads `.td` inputs from disk, resolves local includes first and then the embedded MLIR prelude from `MLIR.Generators`, merges dialect fragments by dialect name using the same merge logic as the source generator, and emits one `.g.cs` file per merged dialect.

Basic usage:

```bash
dotnet run --project tools/TdToCSharp/TdToCSharp.csproj -- <file.td> [more.td ...]
```

Useful options:

- `--stdout`
  Print generated sources to stdout instead of writing files.
- `-o`, `--output <dir>`
  Write generated files to the given directory.
- `--dialect <name>`
  Emit only the named dialect. Can be repeated.
- `--include-prelude`
  Also emit `PreludeDialectRegistration.g.cs`.

Examples:

```bash
dotnet run --project tools/TdToCSharp/TdToCSharp.csproj -- tests/DialectTests/Dialects/arith.td --stdout
dotnet run --project tools/TdToCSharp/TdToCSharp.csproj -- tests/DialectTests/Dialects/arith.td -o artifacts/generated/arith
dotnet run --project tools/TdToCSharp/TdToCSharp.csproj -- a.td b.td --dialect mydialect --include-prelude
```

If you only need to inspect the records after TableGen evaluation, use `tools/TableGenDebug` instead. If you need the final emitted C# for a specific `.td` input, prefer `tools/TdToCSharp`.

## Where To Look Next

- [samples/GeneratedDialectConsumer/Program.cs](/Users/jonathanvdc/Code/MLIR.NET/samples/GeneratedDialectConsumer/Program.cs)
  Small consumer project using generated dialect types.
- [tools/TableGenDebug/Program.cs](/Users/jonathanvdc/Code/MLIR.NET/tools/TableGenDebug/Program.cs)
  Command-line utility for evaluating TableGen files and printing the resulting records.
- [tools/TdToCSharp/Program.cs](/Users/jonathanvdc/Code/MLIR.NET/tools/TdToCSharp/Program.cs)
  Command-line utility for compiling `.td` files into generated C# dialect sources.
- [tests/DialectTests/Dialects/arith.td](/Users/jonathanvdc/Code/MLIR.NET/tests/DialectTests/Dialects/arith.td)
  Real ODS-style dialect fixture used by the analyzer-backed integration tests.
- [AGENTS.md](/Users/jonathanvdc/Code/MLIR.NET/AGENTS.md)
  Repository-specific contributor and agent guidance.
