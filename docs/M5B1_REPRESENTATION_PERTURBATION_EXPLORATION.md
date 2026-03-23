# M5b1 Representation Perturbation Exploration

M5b1 is the first compact representation-robustness pass after M5a1/M5a2r/M5a3.

## Question

If the synthetic tasks stay fixed but the representation pipeline before compression is perturbed in modest, explicit ways, does the coarse compressed-byte value/position neighborhood around `lzmsa-paper` remain qualitatively intact?

## Fixed benchmark scope

- Tasks:
  - `ofdm-signal-present-vs-noise-only`
  - `gaussian-emitter-vs-noise-only`
- SNR grid: `-9`, `-3`, `0` dB
- Window lengths: `64`, `128`
- Seed panel: `86420`, `97531`, `24680`
- Checked-in trial count per condition and class: `72`

## Perturbations tested

M5b1 intentionally keeps the perturbation panel small:

1. `baseline`
   - `sampleScale = 1.0`
   - `numericFormat = float64-le`
2. `scale-half`
   - multiply every sample by `0.5` before serialization
   - keep `float64-le` packing
   - no additional clipping or normalization is applied
3. `float32`
   - keep `sampleScale = 1.0`
   - cast each sample to `float32` before little-endian serialization
   - no additional clipping beyond the IEEE float cast

## Focused feature panel

M5b1 does **not** rerun the full M5a2 feature zoo.

The checked-in focused panel is:

- `lzmsa-paper`
- `lzmsa-mean-compressed-byte-value`
- `lzmsa-compressed-byte-bucket-64-127-proportion`
- `lzmsa-suffix-third-mean-compressed-byte-value`

Representative selection rationale:

- `lzmsa-compressed-byte-bucket-64-127-proportion` is the histogram representative because the checked-in M5a2r compact rerun reported it as the closest simple neighbor.
- `lzmsa-suffix-third-mean-compressed-byte-value` is the positional representative because prior M5a2/M5a3 evidence kept the suffix-third positional family in the nearest-neighbor set even when the single best feature changed.

## Artifact policy

M5b1 is summary-first and uses the repository's existing compact retention path.

- Primary checked-in config uses `artifactRetentionMode = milestone`
- Smoke config uses `artifactRetentionMode = smoke`
- No raw per-trial CSV or raw full ROC tables are emitted by default for the checked-in M5b1 exploration run

Primary compact outputs:

- `m5b1_auc_comparison.csv`
- `m5b1_delta_summary.csv`
- `m5b1_perturbation_stability_summary.csv`
- `m5b1_findings.md`
- `manifest.json`

## Reading guide

Use the artifact set to answer:

1. Which tested feature stays closest to `lzmsa-paper` under each perturbation?
2. Do perturbations merely reshuffle winners inside the same value/position family neighborhood?
3. Or does the neighborhood materially collapse under representation changes?
