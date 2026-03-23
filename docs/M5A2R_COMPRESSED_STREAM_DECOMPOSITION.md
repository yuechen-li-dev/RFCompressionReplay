# M5a2r Compressed-Stream Decomposition Re-land

## What M5a2r Preserves

M5a2r is an operational re-land of the original M5a2 milestone under the current Mx5 artifact-retention policy.

It keeps fixed:

- scalar-window serialization,
- deterministic Brotli compression,
- the same two synthetic binary benchmark tasks used in M4/M4a/M5a1,
- the same readable SNR/window grid (`-9`, `-3`, `0` dB and `64`, `128`),
- the same strengthened `72` trials per class per condition, and
- the same ROC/AUC evaluation layer.

## Intended M5a2 Feature Set

The re-land compares `lzmsa-paper` against the intended second-pass simple compressed-byte summaries:

- `lzmsa-mean-compressed-byte-value`
- `lzmsa-compressed-byte-variance`
- `lzmsa-compressed-byte-bucket-0-63-proportion`
- `lzmsa-compressed-byte-bucket-64-127-proportion`
- `lzmsa-compressed-byte-bucket-128-191-proportion`
- `lzmsa-compressed-byte-bucket-192-255-proportion`
- `lzmsa-prefix-third-mean-compressed-byte-value`
- `lzmsa-suffix-third-mean-compressed-byte-value`

## Required Outputs

An M5a2r-configured run writes the normal evaluation artifacts plus, under the selected retention mode:

- `m5a2_auc_comparison.csv`
  - one row per `(task, snrDb, windowLength)` condition,
  - side-by-side AUCs for `lzmsa-paper` and every intended M5a2 simple-summary detector,
  - per-condition deltas from `lzmsa-paper`.
- `m5a2_delta_summary.csv`
  - compact machine-readable median/max absolute AUC deltas from `lzmsa-paper`,
  - feature-family labels,
  - condition counts where a feature beat whole-stream mean compressed byte value.
- `m5a2_findings.md`
  - scope,
  - concise key finding,
  - compact comparison and aggregate summary tables,
  - caveats.

## Checked-In Configs

- `configs/m5a2r.compressed-stream-decomposition.json`
  - primary M5a2r milestone re-land config,
  - explicit `artifactRetentionMode: "milestone"`,
  - output root `artifacts/m5a2`.
- `configs/m5a2r.compressed-stream-decomposition-smoke.json`
  - tiny regression config,
  - explicit `artifactRetentionMode: "smoke"`.

## Reading Guide

1. Inspect `m5a2_auc_comparison.csv`.
2. Check `m5a2_delta_summary.csv` for median absolute AUC deltas and the counts versus whole-stream mean compressed byte value.
3. Read `m5a2_findings.md` for the cautious artifact-backed interpretation.

## Current Checked-In Re-land Result

The checked-in same-scope re-land under current main did **not** preserve the previously reported unmerged M5a2 headline exactly.

The compact retained artifact set currently shows:

- `lzmsa-compressed-byte-bucket-64-127-proportion` as the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta,
- that detector beating whole-stream mean compressed byte value in `6` of `12` conditions, and
- the coarse histogram family edging out the positional family by best-member median absolute AUC delta.

This should be read as a material change relative to the earlier unmerged suffix-third/7-of-12/coarse-positional summary, not as something the re-land hides.

## Mx5 Retention Note

The checked-in M5a2 re-land keeps only the compact milestone artifact set in git:

- `manifest.json`
- `summary.json`
- `summary.csv`
- `roc_points_compact.csv`
- `m5a2_auc_comparison.csv`
- `m5a2_delta_summary.csv`
- `m5a2_findings.md`

Raw `trials.csv` and raw `roc_points.csv` are intentionally omitted in milestone mode and are expected to be reproducible by rerunning the same config with `artifactRetentionMode: "full"`.
