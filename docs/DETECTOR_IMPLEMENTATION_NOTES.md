# Detector Implementation Notes

This document records the exact detector formulas and serialization contract implemented in this repository through M5a2r.

## Scope of This Document

The goal here is detector-contract clarity, not mechanism interpretation.

The repository now contains:

- the existing ED and CAV detector paths,
- the existing `lzmsa-paper` paper-style byte-sum score path, and
- additional compression-derived score identities that reuse the same serialization and compressed payload basis.

This hardening pass does **not** claim which score identity is scientifically responsible for any later detection effect. It only makes that comparison explicit and testable through the current M4/M5a1/M5a2r mechanism-comparison passes.

## Input Model Used by the Detectors

- Sample representation: scalar `double` values.
- Signal structure: scalar-only; there is no complex IQ layout in the current harness.
- Window ordering: windows are processed in scenario order, then samples are processed in each window's stored order.
- Detector input reduction: detectors flatten all windows in a trial into one ordered scalar sequence.

## Detector Formulas Implemented

### ED (`ed`, mode `average-energy`)

For flattened scalar samples `x[0..N-1]`, the ED score is:

- `score = mean(x_i^2)`

This is the average signal energy over the trial's flattened sample sequence.

Orientation:

- `HigherScoreMorePositive`

### CAV (`cav`, mode `lag-1-absolute-autocovariance`)

For flattened scalar samples `x[0..N-1]` with mean `μ`, the CAV score is:

- `score = abs(mean((x_i - μ) * (x_(i-1) - μ)))` for `i = 1..N-1`

This is a modest lag-1 absolute autocovariance statistic. It is conceptually aligned with covariance-based detection, but it is intentionally documented as a simple harness statistic rather than a claim of full paper-equivalent CAV processing.

Orientation:

- `HigherScoreMorePositive`

### LZMSA paper statistic (`lzmsa-paper`, mode `paper-byte-sum`)

The `lzmsa-paper` score contract is:

1. Serialize the flattened sample sequence into bytes using the explicit serialization contract below.
2. Compress those bytes with the current deterministic compression backend.
3. Compute the score as the sum of the compressed output byte values.

Formula:

- `score = sum(compressedBytes)`

Orientation:

- `HigherScoreMorePositive`

Important caveat:

- This path remains behaviorally unchanged from the earlier M1/M3 implementation.
- The harness reports the paper-style **byte-sum-over-compressed-bytes statistic**.
- It does **not** relabel this score as compressed length or compression ratio.

### LZMSA compressed length (`lzmsa-compressed-length`, mode `compressed-byte-count`)

This variant reuses the same serialized input bytes and same compressed payload as `lzmsa-paper`, but derives the score differently.

Formula:

- `score = compressedByteCount`

Orientation:

- `LowerScoreMorePositive`

Interpretation contract:

- This is the raw compressed output byte count.
- It is intentionally a separate detector identity so configs, artifacts, and ROC/AUC summaries cannot confuse it with the paper-style byte-sum score.

### LZMSA normalized compressed length (`lzmsa-normalized-compressed-length`, mode `compressed-byte-count-per-input-byte`)

This variant again reuses the same serialized input bytes and same compressed payload basis.

Formula:

- `score = compressedByteCount / inputByteCount`

Where:

- `inputByteCount = serialized scalar payload byte count before compression`

Orientation:

- `LowerScoreMorePositive`

Interpretation contract:

- This is a normalized length metric, not a byte-sum statistic.
- It exists so the current comparison milestones can compare score identity while holding the serialization and compression path fixed.

### LZMSA mean compressed byte value (`lzmsa-mean-compressed-byte-value`, mode `mean-compressed-byte-value`)

This M5a1 variant reuses the same serialized input bytes and same compressed payload basis yet again, but derives the score as the mean value of the compressed bytes.

Formula:

- `score = compressedByteSum / compressedByteCount`

Orientation:

- `HigherScoreMorePositive`

Interpretation contract:

- This keeps the compressed payload basis fixed while isolating the second factor in `byteSum = compressedLength × meanCompressedByteValue`.
- The higher-is-more-positive orientation is explicit and intentional: at fixed compressed length, increasing mean compressed byte value increases the paper-style byte-sum score.

### LZMSA compressed byte variance (`lzmsa-compressed-byte-variance`, mode `compressed-byte-variance`)

This M5a2r variant keeps the same compressed payload basis but summarizes the spread of compressed byte values.

Formula:

- `score = mean((compressedByte_i - meanCompressedByteValue)^2)`

Orientation:

- `HigherScoreMorePositive`

### LZMSA coarse histogram bucket proportions

These M5a2r variants summarize the fraction of compressed bytes that fall in one coarse inclusive bucket:

- `lzmsa-compressed-byte-bucket-0-63-proportion`
- `lzmsa-compressed-byte-bucket-64-127-proportion`
- `lzmsa-compressed-byte-bucket-128-191-proportion`
- `lzmsa-compressed-byte-bucket-192-255-proportion`

Formula:

- `score = bucketByteCount / compressedByteCount`

Orientation:

- `HigherScoreMorePositive`

### LZMSA prefix/suffix-third mean compressed byte value

These M5a2r variants keep the same compressed payload basis but summarize coarse byte position:

- `lzmsa-prefix-third-mean-compressed-byte-value`
- `lzmsa-suffix-third-mean-compressed-byte-value`

Formula:

- `score = mean(compressedBytes over first floor(count / 3), minimum 1 when non-empty)`
- `score = mean(compressedBytes over last floor(count / 3), minimum 1 when non-empty)`

Orientation:

- `HigherScoreMorePositive`

## Shared Compression Serialization Contract

The compression-derived variants all share the same explicit, test-locked serialization contract:

- Numeric type: IEEE 754 binary64 `double`.
- Endianness: little-endian.
- Layout: scalar samples only, with no headers, delimiters, metadata, or per-window prefixes.
- Ordering: concatenate samples in trial order as `(window 0 samples) + (window 1 samples) + ...`.
- Quantization/scaling: none beyond the synthetic signal generator's existing rounded `double` values.
- Complex layout: not applicable because signals are scalar-only.

A serialization test locks down representative bytes so that accidental changes to endianness, representation, or packing fail fast.

## Compression Backend Caveat

The current compression backend remains Brotli (`BrotliStream`) as a practical deterministic .NET substitution. This document does **not** claim true LZMA parity. The pre-M4 hardening pass intentionally keeps that backend unchanged so score-identity comparisons do not introduce accidental pipeline drift.

Threshold pass/fail also follows the detector's documented orientation. The legacy `IsAboveThreshold` / `AboveThresholdCount` artifact fields therefore mean “meets the configured detector threshold according to that detector's orientation,” even for lower-is-more-positive detector identities.
