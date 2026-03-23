# M5a1 Compressed-Stream Decomposition

## What M5a1 Compares

M5a1 is the first local decomposition pass on the paper-style compression score.

It keeps fixed:

- scalar-window serialization,
- deterministic Brotli compression,
- the existing synthetic task definitions,
- SNR and window-length sweep semantics,
- deterministic seed derivation, and
- ROC/AUC evaluation.

It then compares four detector identities that all derive from the same serialized input bytes and same compressed payload basis:

- `lzmsa-paper`
- `lzmsa-compressed-length`
- `lzmsa-normalized-compressed-length`
- `lzmsa-mean-compressed-byte-value`

## Main Question

Within the current synthetic harness, does the observed behavior of the paper-style byte-sum score track more closely with compressed length or with mean compressed byte value?

M5a1 operationalizes that question with the identity:

- `byte_sum = compressed_length × mean_compressed_byte_value`

The goal is not to claim a full mechanism. The goal is to identify the nearest simple explanatory neighbor of the existing byte-sum statistic.

## Mean-Compressed-Byte-Value Score Contract

`lzmsa-mean-compressed-byte-value` uses:

- `score = compressed_byte_sum / compressed_byte_count`

Orientation is explicit:

- `HigherScoreMorePositive`

This orientation is chosen deliberately rather than implicitly guessed. Holding compressed length fixed, larger mean compressed byte value increases the paper-style byte-sum score, so the M5a1 decomposition keeps the mean-byte-value factor aligned with the paper statistic's score direction.

## Required Outputs

An M5a1-configured run writes the normal evaluation artifacts plus, under the selected retention mode:

- `m5a1_auc_comparison.csv`
  - one row per `(task, snrDb, windowLength)` condition,
  - side-by-side AUCs for all four compression-derived detector identities,
  - deltas for paper vs length, paper vs normalized length, and paper vs mean byte value.
- `m5a1_findings.md`
  - scope,
  - cautious comparison statement,
  - condition summary,
  - compact comparison table,
  - caveats.
- `m5a1_delta_summary.csv`
  - compact machine-readable median/max absolute AUC delta summary against `lzmsa-paper`.

## Checked-In Configs

- `configs/m5a1.compressed-stream-decomposition.json`
  - primary M5a1 run across both synthetic tasks,
  - explicit `artifactRetentionMode: "milestone"` so checked-in runs keep compact milestone artifacts only,
  - same SNR grid as M4a (`-9`, `-3`, `0` dB),
  - same window-length grid as M4a (`64`, `128`),
  - same strengthened `72` trials per class per condition.
- `configs/m5a1.compressed-stream-decomposition-smoke.json`
  - tiny regression config for runtime/test hygiene,
  - explicit `artifactRetentionMode: "smoke"` for minimal regression outputs.

## Reading Guide

1. Inspect `m5a1_auc_comparison.csv`.
2. Compare `|paper - mean|` against `|paper - compressed length|` per condition.
3. Check `m5a1_delta_summary.csv` for the aggregate median/max absolute AUC deltas.
4. Read `m5a1_findings.md` for the concise artifact-backed interpretation and caveats.

## Caveat Reminder

- Synthetic-only benchmark.
- OFDM-like is not LTE.
- Current compression backend caveat remains in force.
- No SDR claims yet.
- Mechanism is not fully resolved by this pass.

## Mx5 Retention Note

Under Mx5, the checked-in M5a1 milestone run should keep the compact milestone artifact set: manifest, summary artifacts, compact ROC output, comparison CSV, findings markdown, and delta summary. Full per-trial rows and raw threshold-by-threshold ROC CSVs are intentionally omitted from milestone retention and can be regenerated locally by rerunning the same config in `full` retention mode.
