# M4a Confirmation Rerun

## What M4a Is

M4a is a confirmation rerun of the existing M4 score-identity comparison.

It asks the same scientific question again on the same synthetic task family:

- `ofdm-signal-present-vs-noise-only`
- `gaussian-emitter-vs-noise-only`

It keeps fixed:

- scalar-window serialization,
- deterministic Brotli compression,
- detector score formulas,
- ROC/AUC evaluation,
- synthetic task definitions, and
- the three compared detector identities.

## What M4a Changes Relative to M4

M4a intentionally strengthens evidence without moving the goalposts:

- keeps the same two tasks,
- keeps the same three detector identities,
- keeps the same SNR grid (`-9`, `-3`, `0` dB),
- keeps the same window-length grid (`64`, `128`),
- increases trial count per class per condition from `24` to `72`.

That is a 3x per-class Monte Carlo increase while preserving a readable condition matrix.

## Required Outputs

An M4a-configured confirmation run writes the normal evaluation artifacts plus:

- `m4a_auc_comparison.csv`
  - one row per `(task, snrDb, windowLength)` condition,
  - side-by-side AUCs for the three compression-derived detector identities,
  - pairwise AUC deltas.
- `m4a_findings.md`
  - scope and provenance,
  - stability summary,
  - compact comparison table,
  - cautious conclusion,
  - caveats.

## Checked-In Configs

- `configs/m4a.score-identity-confirmation.json`
  - primary M4a confirmation config,
  - same synthetic task matrix as M4,
  - `72` trials per class per condition.
- `configs/m4a.score-identity-smoke.json`
  - tiny regression config for artifact-generation coverage.

## Reading Guide

1. Inspect `m4a_auc_comparison.csv`.
2. Check whether `lzmsa-paper` remains highest across the condition matrix.
3. Check whether the two length-based variants still match or nearly match.
4. Read `m4a_findings.md` for the concise artifact-backed interpretation and caveats.

## Caveat Reminder

- Synthetic-only benchmark.
- OFDM-like is not LTE.
- Current compression backend caveat remains in force.
- No SDR claims yet.
