# TableGen Interpreter Benchmarks

This repository includes a lightweight benchmark harness for `src/TableGen` in `tools/TableGen.Benchmarks`.

## Goals

- measure interpreter-heavy workloads separately from normal `dotnet test` startup/build overhead
- provide stable JSON output that CI and tools can compare
- give pull requests a relative performance view against the PR base commit

## Run Locally

```bash
dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- run --output artifacts/benchmarks/local.json
```

Optional knobs:

- `--warmup N`
- `--iterations N`

## Benchmark Layout

Benchmarks are directory-backed.

- manifests live in `tools/TableGen.Benchmarks/Cases/`
- `.td` inputs live in `tools/TableGen.Benchmarks/Inputs/`

Each manifest describes:

- `name`
- `description`
- `mode`
  Either `evaluate` or `parse-and-evaluate`.
- `source`
  Path to the `.td` input relative to `tools/TableGen.Benchmarks/`.
- `includeResolver`
  Either `none` or `mlir-prelude`.

This keeps the runner small and lets benchmark scenarios evolve without editing benchmark code every time.

## Compare Two Runs

```bash
dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- compare \
  --baseline artifacts/benchmarks/base.json \
  --candidate artifacts/benchmarks/head.json \
  --output artifacts/benchmarks/compare.md
```

## Current Benchmark Set

- `Evaluate.SimpleInheritanceLets`
  Small inherited-field and let-resolution scenario without parse overhead.
- `Evaluate.UtilsTd`
  In-repo `Utils.td`-style helper workload without parse overhead.
- `Evaluate.PreludeMiniDialect`
  MLIR-style dialect/op evaluation against the embedded MLIR prelude without parse overhead.
- `ParseAndEvaluate.PreludeMiniDialect`
  End-to-end parse plus evaluation for a small MLIR-style dialect/op with includes.

## CI Behavior

- Every CI build runs the head benchmark suite and uploads the JSON artifact.
- Pull request builds also run the benchmark suite on the PR base commit.
- PR builds generate a markdown comparison, append it to the GitHub Actions step summary, and publish it as a sticky PR comment for same-repository pull requests.
- Fork-based pull requests may not be able to write PR comments with the default workflow token; when that happens, use the step summary and uploaded artifacts.

## Interpretation Guidance

- `Regression` means the candidate mean time is more than 5% slower than baseline.
- `Improvement` means the candidate mean time is more than 5% faster than baseline.
- `Flat` means the candidate stayed within a 5% noise band.

The 5% threshold is a reporting heuristic, not a hard law. If a change is close to the threshold, rerun locally before drawing a strong conclusion.

## Guidance For Agents

- If you change `src/TableGen/Evaluation`, prefer running the benchmark tool in addition to tests.
- Use the `Evaluate.*` cases to reason about interpreter-only changes and the `ParseAndEvaluate.*` cases to reason about end-to-end user-facing cost.
- Do not claim a performance win from `dotnet test` timing alone; use the benchmark report.
- When adding new interpreter features with non-obvious cost, consider extending the benchmark set with an upstream-shaped scenario that exercises the new path.
- Prefer adding a new manifest and `.td` input instead of hardcoding benchmark content into the runner.
