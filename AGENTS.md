# MLIR Agent Guide

This repository contains a layered MLIR tooling stack:

- `src/TableGen`
  Parses and evaluates TableGen syntax.
- `src/MLIR.ODS`
  Imports interpreted TableGen records into an internal ODS model.
- `src/MLIR.Generators`
  Roslyn incremental source generator that turns ODS models into C#.
- `src/MLIR`
  Runtime library for MLIR CST, parsing, printing, semantics, dialect registration, and transforms.
- `tools/TableGenDebug`
  Debugging utility that loads a TableGen file, evaluates it with the embedded MLIR.Generators prelude, and prints matching records.

## Architecture

The intended flow is:

1. TableGen source -> `TableGen`
2. Interpreted records -> `MLIR.ODS`
3. ODS model -> `MLIR.Generators`
4. Generated C# -> `MLIR`

Prefer implementing features at the earliest correct layer.

- If a `.td` construct cannot be parsed or evaluated correctly, fix `TableGen` first.
- Do not add importer hacks in `MLIR.ODS` to compensate for missing TableGen support.
- Do not add generator hacks in `MLIR.Generators` to compensate for missing ODS model support.

## ODS And TableGen Rules

Prefer actual MLIR ODS/TableGen shapes over repo-local simplifications.

When ODS or TableGen behavior is unclear:

- first check whether the answer is already obvious from local tests or repo conventions
- otherwise consult the mainline MLIR/LLVM ODS/TableGen definitions and documentation instead of inventing a local approximation
- treat upstream MLIR as the semantic reference for syntax and modeling intent unless this repo has an explicit, documented divergence
- if this repo intentionally diverges, document that in tests and code comments

Supported direction of travel:

- `def X : Dialect`
- `class Y_Op<string mnemonic, list<Trait> traits = []> : Op<DialectDef, mnemonic, traits>;`
- `let arguments = (ins ...)`
- `let results = (outs ...)`
- `let assemblyFormat = "..."`

When extending ODS support:

- preserve real inherited base-class structure
- preserve dialect references as record references when appropriate
- keep `cppNamespace`, `summary`, `description`, traits, and declarative assembly format available in the ODS model when they are present
- prefer matching upstream record structure and field names over adding compatibility shims
- add tests that mirror real upstream-style examples when possible

Generated C# namespaces come from `cppNamespace`.

- Split C++ namespaces on `::`
- Pascal-case each segment for C#
- If the first segment is `mlir`, map it to `MLIR`

Example:

- `::mlir::arith` -> `MLIR.Arith`
- `::mlir::foo_bar` -> `MLIR.FooBar`

## Runtime Design

The runtime is intentionally layered:

- CST remains the source of truth for parsing and printing
- semantic operations are typed operation classes, not a separate generic AST wrapper
- custom assembly should be represented as CST transforms, not printer-only behavior

Prefer these boundaries:

- `Parser` parses text into CST
- `Printer` prints CST
- `Binder` binds CST into typed semantic nodes
- `ConcreteSyntaxBuilder` rewrites semantic modules to CST and can be configured to prefer custom assembly or the generic format while deciding whether existing CST nodes should be reused or rebuilt
- `GenericSyntaxBuilder` rewrites custom CST back to generic CST

Keep `Printer` syntax-focused. If a change sounds like "print known ops differently," it probably belongs in a CST transform or dialect assembly hook instead.

## Tests

There are four important test layers:

- `tests/TableGen.Tests`
  Language-level TableGen parsing and evaluation tests.
- `tests/MLIR.Generators.Tests`
  ODS importer and source-generation tests.
- `tests/DialectTests`
  Analyzer-backed integration tests using real `.td` files and generated types.
- `tests/MLIR.Tests`
  Runtime tests for CST, parser, printer, binder, and semantic/runtime behavior.

When changing behavior:

- add `TableGen.Tests` for new language constructs
- add `MLIR.Generators.Tests` for importer/model/emission changes
- add `DialectTests` when generated code should work in a normal consumer build
- add `MLIR.Tests` when runtime behavior changes

## Build And Validation

Preferred validation commands:

- `dotnet test tests/TableGen.Tests/TableGen.Tests.csproj`
- `dotnet test tests/MLIR.Generators.Tests/MLIR.Generators.Tests.csproj`
- `dotnet test tests/DialectTests/DialectTests.csproj`
- `dotnet test tests/MLIR.Tests/MLIR.Tests.csproj`
- `dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- run --output artifacts/benchmarks/local.json`
- `dotnet build samples/GeneratedDialectConsumer/GeneratedDialectConsumer.csproj`
- `dotnet test MLIR.slnx -m:1`

Important:

- Prefer sequential test/build runs when touching `TableGen` or `MLIR.Generators`.
- Parallel `dotnet` runs can cause DLL lock failures in `obj/`.
- If a parallel run fails with "cannot open ... for writing," rerun sequentially before assuming the code is broken.

## Performance Benchmarks

Interpreter-focused benchmarks live in `tools/TableGen.Benchmarks`.

- Use `dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- run --output artifacts/benchmarks/local.json` to generate a local benchmark report.
- On pull requests, CI runs the benchmark tool on both the PR head and the PR base, then publishes a relative comparison in the GitHub Actions step summary.
- On same-repository pull requests, CI also publishes the comparison as a sticky PR comment so the latest benchmark table stays visible on the conversation thread.
- The benchmark JSON is intended to be machine-readable; if you are making `TableGen` interpreter changes, prefer checking the benchmark summary instead of inferring performance from `dotnet test` wall-clock time.
- Treat changes within roughly 5% as noise unless the benchmark scenario is especially stable or repeated measurements show a consistent shift.
- If a change is meant to improve interpreter performance, mention which benchmark cases should move and verify them explicitly before concluding the work helped.
- Benchmark scenarios are directory-backed, not baked into the runner. Add new cases under `tools/TableGen.Benchmarks/Cases/` with their `.td` inputs under `tools/TableGen.Benchmarks/Inputs/`.
- Fork-based pull requests may not have permission to write PR comments with `GITHUB_TOKEN`; in those cases rely on the workflow summary and uploaded artifacts instead of assuming the sticky comment will appear.

## Editing Guidance

- Do not edit `bin/`, `obj/`, `TestResults/`, or generated outputs under `obj/Generated`.
- Keep generated source logic centralized in `src/MLIR.Generators`.
- Prefer adding model richness in `MLIR.ODS` over embedding ad hoc parsing rules in the emitter.
- Prefer explicit tests for new TableGen constructs such as dags, code blocks, record references, traits, or assembly formats.

## Documentation Policy

Treat documentation as part of the implementation, not polish to add only for public APIs.

- Add XML doc comments for non-public types and members when they carry behavior, invariants, caching rules, evaluation order, layering boundaries, or other logic a reader would need to understand the code confidently.
- Optimize comments for reader understanding rather than API formality. Explain responsibilities, data flow, captured assumptions, and why an algorithm is structured the way it is.
- Add inline comments for non-obvious control flow, subtle semantic choices, memoization, scope capture, inheritance merging, parser quirks, or behavior chosen to match upstream MLIR/TableGen semantics.
- Do not add comments that merely restate the code line-by-line. Prefer comments that help a future maintainer build the right mental model.
- When touching older code with weak documentation, improve it as you go, especially around internal helpers and private state that would otherwise require reverse engineering.
- If the repo intentionally diverges from upstream MLIR/TableGen behavior, document that near the code and in tests.

## Samples

`samples/GeneratedDialectConsumer` is the canary for real analyzer usage.

If generator behavior changes, make sure this sample still:

- builds in a normal project
- consumes generated types directly
- uses `.td` files through `AdditionalFiles`

## If Unsure

Ask:

1. Is this a TableGen language issue?
2. Is this an ODS interpretation/modeling issue?
3. Is this a generator emission issue?
4. Is this a runtime/CST/semantic issue?

Put the change in the earliest layer that can express it correctly.
