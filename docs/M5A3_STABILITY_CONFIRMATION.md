# M5a3 Stability Confirmation

## Question

M5a3 is a stability pass over the existing M5a2r feature family. It does **not** expand the detector set or change the synthetic benchmark. It asks a narrower question:

- across a modest explicit seed panel and stronger rerun counts,
- which current M5a2 feature is the closest simple neighbor to `lzmsa-paper`, and
- is that winner stable at the feature level or only at the feature-family level?

## Fixed Scope

M5a3 keeps the current-main M5a2r scope fixed:

- tasks:
  - `ofdm-signal-present-vs-noise-only`
  - `gaussian-emitter-vs-noise-only`
- SNR values: `-9`, `-3`, `0` dB
- window lengths: `64`, `128`
- feature set:
  - `lzmsa-paper`
  - `lzmsa-mean-compressed-byte-value`
  - `lzmsa-compressed-byte-variance`
  - `lzmsa-compressed-byte-bucket-0-63-proportion`
  - `lzmsa-compressed-byte-bucket-64-127-proportion`
  - `lzmsa-compressed-byte-bucket-128-191-proportion`
  - `lzmsa-compressed-byte-bucket-192-255-proportion`
  - `lzmsa-prefix-third-mean-compressed-byte-value`
  - `lzmsa-suffix-third-mean-compressed-byte-value`

It also keeps the existing deterministic serialization and Brotli-backed compression path fixed.

## Strengthening Relative to M5a2r

The checked-in M5a3 config strengthens the rerun without moving the goalposts:

- trials per class per condition increase from `72` to `144`
- explicit seed panel: `86420`, `97531`, `24680`
- retention mode remains milestone-oriented for compact checked-in review

## What to Inspect

An M5a3 run writes top-level stability artifacts plus the underlying per-seed milestone runs.

Top-level artifacts:

- `m5a3_auc_comparison.csv`
  - one row per `(seed, task, snrDb, windowLength)`
  - includes `AUC(lzmsa-paper)`, each current M5a2 feature AUC, and absolute deltas from `lzmsa-paper`
- `m5a3_delta_summary.csv`
  - per-feature median and max absolute AUC delta from `lzmsa-paper`
- `m5a3_stability_summary.csv`
  - per-feature closest-neighbor win count, median absolute delta, max absolute delta, and median closeness rank
- `m5a3_findings.md`
  - concise scope, cautious stability summary, main conclusion, and caveats
- `manifest.json`
  - marks the run as an `m5a3` stability confirmation and records the explicit seed panel

## Reading Guide

1. Start with `m5a3_stability_summary.csv`.
2. Check whether one feature clearly leads in closest-neighbor win count **and** stays strong on median/max delta and median rank.
3. Use `m5a3_auc_comparison.csv` to inspect where any lead is coming from by seed and condition.
4. Read `m5a3_findings.md` for the artifact-backed interpretation.

## Intended Interpretation Boundary

M5a3 can support a statement such as:

- a single current M5a2 feature stayed the most stable closest neighbor, or
- no single feature stayed stable and the conclusion is only that a coarse bytestream-value / positional family remains informative.

M5a3 does **not** solve the full mechanism. It remains synthetic-only, OFDM-like is not LTE, the current compression-backend caveat remains, and no SDR claim is supported here.
