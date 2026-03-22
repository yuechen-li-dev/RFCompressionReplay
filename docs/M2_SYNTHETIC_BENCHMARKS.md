# M2 Synthetic Benchmark Notes

## Purpose

M2 introduces the first synthetic benchmark scaffold that is strong enough to compare the currently implemented detectors under controlled truth conditions.

The benchmark currently focuses on:

- `noise-only` versus `signal-present`
- `gaussian-emitter` versus `ofdm-like`

That lets the harness probe both energy sensitivity and structure sensitivity without over-claiming standards fidelity.

## Synthetic Case Definitions

### Gaussian noise

The baseline stream is generated from a seeded Gaussian process parameterized by:

- `mean`
- `standardDeviation`
- `baseStreamLength`

This is the shared background noise source for all M2 synthetic runs.

### Gaussian-emitter control

The Gaussian-emitter control is defined here as an **independent Gaussian signal process** mixed into Gaussian background noise.

This choice is deliberate:

- it keeps the signal-present control Gaussian-like
- it avoids introducing obvious subcarrier or periodic structure
- it helps distinguish detectors that mostly respond to energy from detectors that respond differently when more structure is present

It is a control signal, not a real transmitter model.

### OFDM-like structured source

The OFDM-like generator is a deterministic synthetic approximation, not LTE.

High-level construction:

1. Choose a configured number of subcarriers.
2. For each symbol interval and subcarrier, generate a seeded BPSK-like symbol (`+1` or `-1`).
3. For each sample inside the symbol interval, sum cosine subcarriers at configured normalized spacing.
4. Normalize by `sqrt(subcarrierCount)` and scale by the configured amplitude.

This produces a real-valued stream with visibly more organization than the Gaussian-emitter control while remaining easy to audit.

## SNR Definition

For signal-bearing synthetic cases, the harness uses the finite-stream definition:

`SNR_dB = 10 * log10(P_signal / P_noise)`

where `P_signal` and `P_noise` are average powers computed over the exact generated signal and noise arrays.

The signal array is scaled so the finite generated stream matches the requested target SNR before mixing.

## Windowing Approach

M2 uses deterministic consecutive windows:

- generate one longer base stream per synthetic case
- draw seeded start indices
- extract a contiguous span of `sampleWindowCount * samplesPerWindow`
- split that span into consecutive windows of equal length

This keeps the trial sampling explicit and repeatable.

## Fidelity Notes

What is faithful to the paper's experimental logic:

- comparing detectors on signal-present versus noise-only tasks
- including a Gaussian-like control case in addition to a more structured signal case
- keeping synthetic truth explicit and reproducible

What remains provisional or independently regenerated:

- the exact Gaussian-emitter control construction
- the exact OFDM-like waveform details
- any detector threshold interpretation beyond simple score reporting
- any future ROC/AUC or figure-level comparisons
