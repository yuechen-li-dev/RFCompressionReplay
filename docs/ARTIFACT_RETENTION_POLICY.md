# Artifact Retention Policy (Mx5)

## Purpose

Mx5 adds a repository-native artifact retention policy for this lab's experiment harness.

The goal is repository hygiene without weakening auditability:

- keep checked-in milestone artifacts compact,
- keep scientific conclusions legible,
- keep reproducibility explicit, and
- avoid treating git like a raw data lake.

Mx5 is an infrastructure milestone. It does **not** change detector logic, task definitions, evaluation formulas, or the scientific interpretation of prior milestone results.

## Retention Modes

Configs now declare `artifactRetentionMode` explicitly.

### `full`

Use for local exploratory work and debugging.

Retains the fuller raw artifact set, including:

- `manifest.json`
- `summary.json`
- `summary.csv`
- `trials.csv`
- `roc_points.csv`
- milestone comparison/findings files when the run produces them

### `milestone`

Use for checked-in milestone preservation.

Retains only compact, reproducibility-critical, conclusion-critical artifacts:

- `manifest.json`
- `summary.json`
- `summary.csv`
- `roc_points_compact.csv`
- milestone findings markdown when produced
- compact comparison CSVs such as:
  - `m4_auc_comparison.csv`
  - `m4a_auc_comparison.csv`
  - `m5a1_auc_comparison.csv`
  - `m5a1_delta_summary.csv`

Intentionally omitted by default in milestone mode:

- `trials.csv`
- `roc_points.csv`
- other large raw intermediate dumps if they are redundant with compact retained summaries

### `smoke`

Use for CI/regression coverage.

Retains the minimal compact set needed to prove the run succeeded and special report builders still work:

- `manifest.json`
- `summary.json`
- `summary.csv`
- milestone comparison/findings files when produced

Smoke mode omits both raw and compact ROC exports as well as per-trial rows.

## Compact ROC Policy

Raw threshold-by-threshold ROC CSVs can grow quickly while adding little value to checked-in milestone folders.

In `milestone` mode the harness now writes:

- `roc_points_compact.csv`

This compact ROC file keeps a deterministic fixed-size downsample per `(task, detector, snrDb, windowLength)` condition and records the original `sourcePointCount` so reviewers can see that compaction occurred.

The raw `roc_points.csv` file is omitted in milestone mode.

## Reproducibility Expectations

Mx5 preserves scientific sufficiency, not archival exhaust.

The manifest records:

- the chosen retention mode,
- the retained artifact paths,
- which artifact families were intentionally omitted, and
- a regeneration note explaining how to recover omitted raw artifacts.

Omitted raw artifacts are expected to be reproducible from:

- checked-in config,
- checked-in code revision,
- recorded seed,
- manifest metadata, and
- the same retention mode switched to `full` when fuller local output is needed.

## Checked-In Milestone Practice

Milestone artifact directories in `configs/artifacts/` should keep the compact retained set only.

For current checked-in milestone-style runs, that means preserving:

- manifest,
- compact summaries,
- findings markdown,
- compact comparison CSVs, and
- compact ROC output where it materially helps auditing.

It does **not** mean preserving full per-trial exhaust in git.

## How To Regenerate Full Artifacts

Run the same config with:

- `artifactRetentionMode: "full"`

For example:

```bash
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a1.compressed-stream-decomposition.json
```

and temporarily change the config's `artifactRetentionMode` from `milestone` to `full` for the local rerun.

That rerun will regenerate omitted raw artifacts such as `trials.csv` and `roc_points.csv` without changing the scientific benchmark definition.
