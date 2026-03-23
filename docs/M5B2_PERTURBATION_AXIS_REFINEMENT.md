# M5b2 Perturbation-Axis Refinement

M5b2 is a compact refinement pass over M5b1.

## Question

M5b1 showed that the coarse `lzmsa-paper` neighborhood survives modest representation perturbations, but the single-feature winner reshuffled across perturbations. M5b2 asks which perturbation axis is doing most of that reshuffling:

- numeric scaling changes,
- packing / precision changes, or
- both.

## Fixed Scope

M5b2 keeps the following fixed relative to M5b1:

- synthetic tasks:
  - `ofdm-signal-present-vs-noise-only`
  - `gaussian-emitter-vs-noise-only`
- readable condition grid:
  - SNRs `-9`, `-3`, `0` dB
  - window lengths `64`, `128`
- focused feature panel:
  - `lzmsa-paper`
  - `lzmsa-mean-compressed-byte-value`
  - `lzmsa-compressed-byte-bucket-64-127-proportion`
  - `lzmsa-suffix-third-mean-compressed-byte-value`
- current deterministic compression backend
- compact retention-first artifact policy

## Perturbation Axes

The checked-in M5b2 config separates perturbations into explicit axis tags:

1. `baseline`
   - `sampleScale = 1.0`
   - `numericFormat = float64-le`
2. `scale`
   - `sampleScale = 0.5`
   - `numericFormat = float64-le`
3. `packing`
   - `sampleScale = 1.0`
   - `numericFormat = float32-le`
4. `combined`
   - `sampleScale = 0.5`
   - `numericFormat = float32-le`

No extra clipping or normalization is added beyond the selected IEEE float representation.

## Compact Outputs

M5b2 keeps only the compact artifacts needed for decision-making by default:

- `manifest.json`
- `m5b2_auc_comparison.csv`
- `m5b2_delta_summary.csv`
- `m5b2_axis_summary.csv`
- `m5b2_findings.md`

The intent is summary-first comparison against M5b1, not retention of large raw dumps.

## Reading Guide

- Use `m5b2_auc_comparison.csv` for per-seed, per-condition AUC and absolute-delta comparisons.
- Use `m5b2_delta_summary.csv` for per-perturbation compact median/max delta summaries.
- Use `m5b2_axis_summary.csv` for direct baseline-vs-scale-vs-packing-vs-combined comparison of closest-neighbor win counts and median/max deltas.
- Use `m5b2_findings.md` for the cautious axis-level read, family-level interpretation, and caveats.
