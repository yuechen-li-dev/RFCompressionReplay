# M5b3 Scale-Handling Refinement

M5b3 is a compact refinement pass over M5b2.

## Question

M5b2 suggested that scale-only perturbations matter more than packing-only perturbations for the single-feature winner. M5b3 asks:

- how the nearest-neighbor ranking changes across a small explicit scale panel,
- whether one feature becomes more competitive at lower or higher scales, and
- whether one simple normalization rule reduces that scale sensitivity.

## Fixed Scope

M5b3 keeps the following fixed relative to M5b2:

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

## Scale Panel and Representation Families

The checked-in M5b3 config tests this explicit scale panel:

- `0.5x`
- `1.0x`
- `2.0x`
- `4.0x`

It compares two representation families:

1. `raw-scaled`
   - apply the tested scale directly
   - serialize as `float64-le`
   - no extra normalization
2. `normalized-rms`
   - apply the tested scale directly
   - normalize each window to target RMS `1.0`
   - serialize as `float64-le`

Only one normalization rule is introduced here: per-window RMS normalization to a fixed target before serialization.

## Compact Outputs

M5b3 keeps only the compact artifacts needed for decision-making by default:

- `manifest.json`
- `m5b3_auc_comparison.csv`
- `m5b3_delta_summary.csv`
- `m5b3_scale_summary.csv`
- `m5b3_findings.md`

The intent is summary-first comparison against M5b2, not retention of large raw dumps.

## Reading Guide

- Use `m5b3_auc_comparison.csv` for per-family, per-scale, per-seed, per-condition AUC and absolute-delta comparisons.
- Use `m5b3_delta_summary.csv` for overall raw-vs-normalized closest-neighbor counts and aggregate median/max delta summaries.
- Use `m5b3_scale_summary.csv` for direct per-scale comparison of closest-neighbor win counts, median/max deltas, and scale-level leaders.
- Use `m5b3_findings.md` for the cautious scale-sensitivity read, normalization read, family-level interpretation, and caveats.
