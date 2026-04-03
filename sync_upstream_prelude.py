#!/usr/bin/env python3
"""
Synchronize src/MLIR.Generators/Prelude/Upstream with the upstream MLIR TableGen
include files from https://github.com/llvm/llvm-project/tree/main/mlir/include/mlir.

Only .td files are fetched; all other file types are ignored.

The script performs a shallow, sparse clone of llvm/llvm-project (no full history,
only the mlir/include/mlir subtree) into a temporary directory, then copies every
.td file it finds into the Prelude/Upstream directory, mirroring the same relative
path structure.  The temporary clone is removed at the end.

Usage:
    python3 sync_upstream_prelude.py [--ref <git-ref>]

Options:
    --ref     Git ref (branch, tag, or SHA) to sync from. Defaults to 'main'.
"""

import argparse
import os
import shutil
import subprocess
import sys
import tempfile

REPO_URL = "https://github.com/llvm/llvm-project"
# Path inside the upstream repo that contains the .td files we want.
SYNC_ROOT = "mlir/include/mlir"
# The prefix to strip from each upstream path to get the Upstream/-relative path.
# e.g.  "mlir/include/mlir/IR/OpBase.td"  →  "mlir/IR/OpBase.td"
UPSTREAM_PREFIX = "mlir/include/"

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
UPSTREAM_DIR = os.path.join(
    SCRIPT_DIR,
    "src",
    "MLIR.Generators",
    "Prelude",
    "Upstream",
)


def run(args: list[str], **kwargs) -> None:
    """Run a subprocess, raising on non-zero exit."""
    subprocess.run(args, check=True, **kwargs)


def sparse_clone(ref: str, dest: str) -> None:
    """Perform a shallow sparse clone of the upstream repo into *dest*."""
    print(f"Cloning {REPO_URL} @ {ref} (sparse, depth=1) …")
    run(
        [
            "git", "clone",
            "--depth=1",
            "--filter=blob:none",
            "--sparse",
            "--branch", ref,
            REPO_URL,
            dest,
        ],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
    )
    # Limit the working tree to only the subtree we care about.
    run(
        ["git", "sparse-checkout", "set", SYNC_ROOT],
        cwd=dest,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
    )
    print("Sparse checkout complete.")


def sync(ref: str) -> None:
    tmp = tempfile.mkdtemp(prefix="llvm-sparse-")
    try:
        sparse_clone(ref, tmp)

        src_root = os.path.join(tmp, SYNC_ROOT)
        td_files: list[str] = []
        for dirpath, _, filenames in os.walk(src_root):
            for name in filenames:
                if name.endswith(".td"):
                    td_files.append(os.path.join(dirpath, name))

        print(f"Found {len(td_files)} .td file(s) under {SYNC_ROOT}/")

        for src_path in sorted(td_files):
            # Compute path relative to the tmp clone root.
            repo_rel = os.path.relpath(src_path, tmp)  # e.g. mlir/include/mlir/IR/OpBase.td
            # Use forward slashes for cross-platform robustness.
            repo_rel = repo_rel.replace(os.sep, "/")
            # Strip the prefix to get the Upstream/-relative path.
            upstream_rel = repo_rel.removeprefix(UPSTREAM_PREFIX)  # e.g. mlir/IR/OpBase.td
            dest = os.path.join(UPSTREAM_DIR, upstream_rel.replace("/", os.sep))
            print(f"  {upstream_rel}")
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            shutil.copy2(src_path, dest)

        print(f"\nSynced {len(td_files)} file(s) to {UPSTREAM_DIR}")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Sync upstream MLIR .td files into the Prelude/Upstream directory."
    )
    parser.add_argument(
        "--ref",
        default="main",
        help="Git ref (branch, tag, or SHA) to sync from (default: main).",
    )
    args = parser.parse_args()

    if shutil.which("git") is None:
        print("ERROR: git is not available on PATH.", file=sys.stderr)
        sys.exit(1)

    sync(args.ref)


if __name__ == "__main__":
    main()
