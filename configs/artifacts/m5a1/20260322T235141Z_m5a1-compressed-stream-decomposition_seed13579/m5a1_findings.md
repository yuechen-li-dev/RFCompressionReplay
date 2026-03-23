# M5a1 Compressed-Stream Decomposition Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 72
- Detector identities compared: lzmsa-paper, lzmsa-compressed-length, lzmsa-normalized-compressed-length, lzmsa-mean-compressed-byte-value
- Mean-byte-value orientation: HigherScoreMorePositive (chosen explicitly because byte-sum = compressed-length × mean-byte-value, so higher mean byte value increases the paper-style byte-sum when the compressed-length factor is held fixed)
- Seed: 13579
- Config provenance: m5a1-compressed-stream-decomposition / M5a1 Synthetic Compressed-Stream Decomposition

## Main Comparison Statement

- Within the current synthetic benchmark, mean compressed byte value tracked the paper-style byte-sum score more closely than compressed length did.

## Condition Summary

- The mean-byte-value variant was closer to `lzmsa-paper` than raw compressed length in most conditions (11 of 12).
- `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` matched exactly in every tested condition.

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC compressed length | AUC normalized length | AUC mean byte value | Δ paper-length | Δ paper-normalized | Δ paper-mean |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.453800 | 0.451389 | 0.451389 | 0.446084 | 0.002411 | 0.002411 | 0.007716 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.489583 | 0.463638 | 0.463638 | 0.492284 | 0.025945 | 0.025945 | -0.002701 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.525752 | 0.232542 | 0.232542 | 0.465278 | 0.293210 | 0.293210 | 0.060474 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.624807 | 0.033661 | 0.033661 | 0.517940 | 0.591146 | 0.591146 | 0.106867 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.665509 | 0.126543 | 0.126543 | 0.582755 | 0.538966 | 0.538966 | 0.082754 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.643519 | 0.032407 | 0.032407 | 0.526813 | 0.611112 | 0.611112 | 0.116706 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.500772 | 0.355806 | 0.355806 | 0.481481 | 0.144966 | 0.144966 | 0.019291 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.486400 | 0.307870 | 0.307870 | 0.450231 | 0.178530 | 0.178530 | 0.036169 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.630594 | 0.145640 | 0.145640 | 0.561921 | 0.484954 | 0.484954 | 0.068673 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.650559 | 0.060282 | 0.060282 | 0.565490 | 0.590277 | 0.590277 | 0.085069 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.659144 | 0.076389 | 0.076389 | 0.560957 | 0.582755 | 0.582755 | 0.098187 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.630208 | 0.027778 | 0.027778 | 0.489969 | 0.602430 | 0.602430 | 0.140239 |

## Aggregate Delta Summary

| Alternative detector | Median | Max |
| --- | ---: | ---: |
| lzmsa-compressed-length | 0.511960 | 0.611112 |
| lzmsa-normalized-compressed-length | 0.511960 | 0.611112 |
| lzmsa-mean-compressed-byte-value | 0.075713 | 0.140239 |

## Cautious Interpretation

- Within the current synthetic benchmark, mean compressed byte value tracked the paper-style byte-sum score more closely than compressed length did. This narrows the local mechanism question, but it does not fully resolve it.

## Caveats

- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.
- This decomposition pass does not fully resolve mechanism; it only checks whether mean compressed byte value is a closer simple neighbor of byte-sum than length-based scoring is.

## Artifact Notes

- Per-trial score rows: 6912
- Per-condition summary rows: 48
- ROC point rows: 4017
