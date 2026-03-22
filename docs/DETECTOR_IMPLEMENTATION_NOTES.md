# M1 Detector Implementation Notes

This document records the exact detector formulas and serialization contract implemented for M1.

## Scope of M1

M1 replaces the M0 placeholder detector path with real detector implementations over the existing deterministic synthetic signal windows. It does **not** claim paper-figure reproduction yet.

## Input Model Used in M1

- Sample representation: scalar `double` values.
- Signal structure: scalar-only; there is no complex IQ layout in M1.
- Window ordering: windows are processed in scenario order, then samples are processed in each window's stored order.
- Detector input reduction: detectors flatten all windows in a trial into one ordered scalar sequence.

## Detector Formulas Implemented

### ED (`ed`, mode `average-energy`)

For flattened scalar samples `x[0..N-1]`, the M1 ED score is:

- `score = mean(x_i^2)`

This is the average signal energy over the trial's flattened sample sequence.

### CAV (`cav`, mode `lag-1-absolute-autocovariance`)

For flattened scalar samples `x[0..N-1]` with mean `μ`, the M1 CAV score is:

- `score = abs(mean((x_i - μ) * (x_(i-1) - μ)))` for `i = 1..N-1`

This is a modest lag-1 absolute autocovariance statistic. It is conceptually aligned with covariance-based detection, but it is intentionally documented as a simple harness statistic rather than a claim of full paper-equivalent CAV processing.

### LZMSA paper statistic (`lzmsa-paper`, mode `paper-byte-sum`)

The M1 `lzmsa-paper` score contract is:

1. Serialize the flattened sample sequence into bytes using the explicit serialization contract below.
2. Compress those bytes with the currently configured deterministic compression backend.
3. Compute the score as the sum of the compressed output byte values.

Important caveat:

- The harness reports the paper-style **byte-sum-over-compressed-bytes statistic**.
- It does **not** relabel this as compressed length or compression ratio.
- In M1, the compression backend is Brotli (`BrotliStream`) as a practical deterministic .NET substitution, not a claim of true LZMA parity.

## Compression Serialization Contract

The M1 serialization contract is explicit and test-locked:

- Numeric type: IEEE 754 binary64 `double`.
- Endianness: little-endian.
- Layout: scalar samples only, with no headers, delimiters, metadata, or per-window prefixes.
- Ordering: concatenate samples in trial order as `(window 0 samples) + (window 1 samples) + ...`.
- Quantization/scaling: none beyond the synthetic signal generator's existing rounded `double` values.
- Complex layout: not applicable in M1 because signals are scalar-only.

A serialization test locks down representative bytes so that accidental changes to endianness, representation, or packing fail fast.

## Faithful vs Provisional in M1

Faithful to the paper conceptually:

- The harness now contains explicit ED, covariance-style, and compression-statistic detector paths.
- The `lzmsa-paper` score is modeled as a byte-sum-over-compressed-bytes statistic, which is the key paper distinction needed for later milestones.

Still provisional in this independent reproduction:

- The synthetic signal source is still a deterministic stand-in, not a realistic RF/LTE generator.
- The CAV statistic is a simple lag-1 variant.
- The compression backend is Brotli rather than a dedicated LZMA implementation.
- M1 does not attempt paper-figure replication, ROC/AUC sweeps, or benchmark breadth.
