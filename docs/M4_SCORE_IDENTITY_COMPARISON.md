# M4 Score-Identity Comparison

## What M4 Compares

M4 is the first mechanism-comparison experiment in this repository.

It runs the existing M3 synthetic evaluation tasks and conditions while holding the following pieces fixed:

- scalar-window serialization,
- deterministic Brotli compression,
- synthetic task definitions,
- SNR and window-length sweep semantics,
- deterministic seed derivation, and
- ROC/AUC evaluation.

It then compares three detector identities that differ only in the score extracted from the same compressed payload:

- `lzmsa-paper`
- `lzmsa-compressed-length`
- `lzmsa-normalized-compressed-length`

## Main Question

Within the repository's synthetic benchmark, does the observed detection effect track the paper-style byte-sum statistic specifically, or does it survive when the score identity is replaced by true compression-length-based metrics?

## Required Outputs

An M4-configured comparison run writes the normal M3 artifacts plus:

- `m4_auc_comparison.csv`
  - one row per `(task, snrDb, windowLength)` condition,
  - side-by-side AUCs for the three compression-derived detector identities,
  - pairwise AUC deltas.
- `m4_findings.md`
  - experimental scope,
  - compact comparison table,
  - short findings text backed by the generated artifacts,
  - explicit caveats.

## Checked-In Configs

- `configs/m4.score-identity-comparison.json`
  - primary compact M4 run across both synthetic tasks,
  - three SNR points (`-9`, `-3`, `0` dB),
  - two window lengths (`64`, `128`),
  - `24` trials per class per condition.
- `configs/m4.score-identity-smoke.json`
  - tiny regression config used to keep test/runtime hygiene reasonable.

## How To Read The Outputs

1. Inspect `m4_auc_comparison.csv` first.
2. Check whether pairwise deltas remain small or grow in specific tasks/conditions.
3. Read `m4_findings.md` for the concise artifact-backed interpretation and caveats.

## Caveat Reminder

- Synthetic-only benchmark.
- OFDM-like is not LTE.
- Current compression backend caveat remains in force.
- No SDR claims yet.
