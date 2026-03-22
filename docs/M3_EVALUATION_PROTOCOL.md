# M3 Evaluation Protocol Notes

## Purpose

M3 adds a deterministic synthetic evaluation loop that mirrors the paper's comparison shape without claiming paper-number reproduction.

The protocol now supports:

- binary benchmark tasks with explicit positive and negative class definitions,
- SNR sweeps,
- window-length sweeps,
- repeated Monte Carlo trials per condition,
- continuous detector score collection, and
- ROC/AUC generation from score distributions.

The current pre-M4 hardening pass keeps that evaluation protocol in place and makes compression-score identity explicit so a later M4 run can compare detector-score semantics on the same tasks without changing the evaluation layer midstream.

## Binary Tasks Used in This Repository

### `ofdm-signal-present-vs-noise-only`

- **Positive class:** OFDM-like structured synthetic signal mixed into Gaussian background noise.
- **Negative class:** Gaussian noise only.
- **SNR meaning:** the configured SNR applies to the positive signal-plus-noise construction. The negative baseline has no intrinsic SNR, but trial artifacts still record the condition SNR so the paired condition remains explicit.

### `gaussian-emitter-vs-noise-only`

- **Positive class:** independent Gaussian emitter mixed into Gaussian background noise.
- **Negative class:** Gaussian noise only.
- **SNR meaning:** same convention as above; the positive case uses the configured SNR, while the negative baseline is paired under that named condition.

This control task is intentionally honest about what it is: a signal-present case that remains Gaussian-like rather than a realistic transmitter model.

## Sweep Semantics

### SNR sweep

M3 configs provide an explicit list of `evaluation.snrDbValues`.

For signal-bearing positive cases, each listed SNR value overrides the positive case template's configured `snrDb` for that condition. For `noise-only` baselines, the source itself remains SNR-free, but artifacts still carry the **condition SNR** so reviewers can inspect the paired ROC/AUC condition clearly.

### Window-length sweep

M3 configs provide an explicit list of `evaluation.windowLengths`.

Each length is applied to deterministic consecutive-window extraction while keeping:

- the benchmark base stream,
- the noise and signal generator parameters, and
- the scenario's `sampleWindowCount`

otherwise fixed.

### Monte Carlo trials

`evaluation.trialCountPerCondition` defines how many positive examples and how many negative examples are scored for each `(task, detector, snrDb, windowLength)` condition.

The harness uses deterministic derived seeds for:

- base-stream generation, and
- window start-index generation.

This keeps the evaluation repeatable without hidden global mutable state.

## Compression-Derived Score Identities in Evaluation

The evaluation layer now accepts three separate compression-derived detector IDs:

- `lzmsa-paper`
- `lzmsa-compressed-length`
- `lzmsa-normalized-compressed-length`

All three share the same serialized-input and compressed-payload basis. They differ only in score derivation:

- `lzmsa-paper`: `sum(compressedBytes)`
- `lzmsa-compressed-length`: `compressedByteCount`
- `lzmsa-normalized-compressed-length`: `compressedByteCount / inputByteCount`

This pre-M4 hardening pass does **not** interpret which identity matters scientifically. It only ensures configs, validation, artifacts, and ROC/AUC can treat them as separate, explicit detector paths.

## ROC/AUC Method

The ROC/AUC implementation is intentionally modest and explicit:

1. Collect binary labels and detector scores for one condition.
2. Sort scores according to the detector's documented score orientation.
3. Walk distinct score thresholds.
4. Emit ROC points with cumulative true-positive and false-positive counts.
5. Compute AUC with the trapezoidal rule over the ordered ROC points.

Tied scores are handled deterministically by processing equal-score groups together.

## Score Orientation Assumptions

M3 records score orientation explicitly in detector metadata, per-trial rows, per-condition summaries, and ROC artifacts.

Current documented orientation:

- `ed`: higher score => more positive
- `cav`: higher score => more positive
- `lzmsa-paper`: higher score => more positive
- `lzmsa-compressed-length`: lower score => more positive
- `lzmsa-normalized-compressed-length`: lower score => more positive

The implementation does **not** silently assume a universal direction without documentation and tests.

## Artifact Shape

### `trials.csv`

Each row records at least:

- `trialIndex`
- `taskName`
- `targetLabel` / `classLabel`
- `detectorName`
- `detectorMode`
- `scoreOrientation`
- `conditionSnrDb`
- `sourceSnrDb`
- `windowLength`
- `score`

### `summary.csv` and `summary.json`

Grouped at least by:

- task
- detector
- SNR condition
- window length

with:

- positive and negative class counts
- min/max/mean/std of scores
- above-threshold count
- AUC

### `roc_points.csv`

Contains per-condition ROC rows with:

- task
- detector
- detector mode
- score orientation
- SNR condition
- window length
- threshold
- TPR
- FPR
- class counts
- AUC

## Faithful vs Provisional

What mirrors the paper's experiment shape:

- signal-present versus noise-only comparison
- Gaussian-like control versus the same baseline
- comparison across SNR and window-length conditions
- ROC/AUC from score distributions rather than ad hoc single thresholds

What remains provisional and independently regenerated:

- the exact Gaussian-emitter control construction
- the exact OFDM-like waveform details
- any LTE fidelity claim
- exact paper-number reproduction
- any mechanism interpretation across the compression-derived score identities
