# M5a2 Compressed-Stream Decomposition Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 144
- Detector identities compared: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value
- Seed: 97531
- Config provenance: m5a3-stability-confirmation / M5a3 Synthetic Stability Confirmation

## Main Comparison Statement

- Within the current synthetic benchmark, `lzmsa-mean-compressed-byte-value` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta.

## Condition Summary

- `lzmsa-mean-compressed-byte-value` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta (0.063875).
- It was closer to `lzmsa-paper` than whole-stream mean compressed byte value in 0 of 12 tested conditions.
- The most informative tested feature family by best-member median absolute AUC delta was `whole-stream`.

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC mean | AUC variance | AUC bucket 0-63 | AUC bucket 64-127 | AUC bucket 128-191 | AUC bucket 192-255 | AUC prefix-third mean | AUC suffix-third mean | Δ paper-mean | Δ paper-variance | Δ paper-bucket 0-63 | Δ paper-bucket 64-127 | Δ paper-bucket 128-191 | Δ paper-bucket 192-255 | Δ paper-prefix-third | Δ paper-suffix-third |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.566503 | 0.554398 | 0.500289 | 0.428337 | 0.524643 | 0.482615 | 0.541016 | 0.492718 | 0.538508 | 0.012105 | 0.066214 | 0.138166 | 0.041860 | 0.083888 | 0.025487 | 0.073785 | 0.027995 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.546851 | 0.550010 | 0.582079 | 0.500096 | 0.477840 | 0.432677 | 0.601587 | 0.500675 | 0.541040 | -0.003159 | -0.035228 | 0.046755 | 0.069011 | 0.114174 | -0.054736 | 0.046176 | 0.005811 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.635296 | 0.563561 | 0.456501 | 0.478926 | 0.456187 | 0.531877 | 0.520833 | 0.503376 | 0.565008 | 0.071735 | 0.178795 | 0.156370 | 0.179109 | 0.103419 | 0.114463 | 0.131920 | 0.070288 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.667149 | 0.613474 | 0.462432 | 0.403091 | 0.514540 | 0.529490 | 0.547671 | 0.586251 | 0.553819 | 0.053675 | 0.204717 | 0.264058 | 0.152609 | 0.137659 | 0.119478 | 0.080898 | 0.113330 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.646774 | 0.572000 | 0.447193 | 0.395231 | 0.544488 | 0.516300 | 0.523558 | 0.538291 | 0.561921 | 0.074774 | 0.199581 | 0.251543 | 0.102286 | 0.130474 | 0.123216 | 0.108483 | 0.084853 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.718750 | 0.604335 | 0.448881 | 0.388817 | 0.573881 | 0.463566 | 0.582345 | 0.559606 | 0.610050 | 0.114415 | 0.269869 | 0.329933 | 0.144869 | 0.255184 | 0.136405 | 0.159144 | 0.108700 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.407070 | 0.407456 | 0.498601 | 0.561970 | 0.529466 | 0.458430 | 0.479504 | 0.439501 | 0.428361 | -0.000386 | -0.091531 | -0.154900 | -0.122396 | -0.051360 | -0.072434 | -0.032431 | -0.021291 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.559920 | 0.524354 | 0.506125 | 0.485894 | 0.453631 | 0.524040 | 0.524764 | 0.536024 | 0.483700 | 0.035566 | 0.053795 | 0.074026 | 0.106289 | 0.035880 | 0.035156 | 0.023896 | 0.076220 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.560354 | 0.504340 | 0.453848 | 0.472970 | 0.499132 | 0.533782 | 0.484857 | 0.468412 | 0.556641 | 0.056014 | 0.106506 | 0.087384 | 0.061222 | 0.026572 | 0.075497 | 0.091942 | 0.003713 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.562331 | 0.481819 | 0.456163 | 0.482735 | 0.506559 | 0.524884 | 0.482904 | 0.488474 | 0.460503 | 0.080512 | 0.106168 | 0.079596 | 0.055772 | 0.037447 | 0.079427 | 0.073857 | 0.101828 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.603684 | 0.501591 | 0.443625 | 0.467568 | 0.500747 | 0.549069 | 0.474151 | 0.492863 | 0.560282 | 0.102093 | 0.160059 | 0.136116 | 0.102937 | 0.054615 | 0.129533 | 0.110821 | 0.043402 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.646991 | 0.530237 | 0.410204 | 0.477865 | 0.502267 | 0.554832 | 0.484881 | 0.497541 | 0.521460 | 0.116754 | 0.236787 | 0.169126 | 0.144724 | 0.092159 | 0.162110 | 0.149450 | 0.125531 |

## Aggregate Delta Summary

| Alternative detector | Family | Median | Max | Conditions closer than whole-stream mean |
| --- | --- | ---: | ---: | ---: |
| lzmsa-mean-compressed-byte-value | whole-stream | 0.063875 | 0.116754 | 0 |
| lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.073254 | 0.125531 | 4 |
| lzmsa-prefix-third-mean-compressed-byte-value | coarse-positional | 0.086420 | 0.159144 | 2 |
| lzmsa-compressed-byte-bucket-128-191-proportion | coarse-histogram | 0.088024 | 0.255184 | 4 |
| lzmsa-compressed-byte-bucket-192-255-proportion | coarse-histogram | 0.096945 | 0.162110 | 2 |
| lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.104613 | 0.179109 | 1 |
| lzmsa-compressed-byte-variance | whole-stream | 0.133283 | 0.269869 | 0 |
| lzmsa-compressed-byte-bucket-0-63-proportion | coarse-histogram | 0.146533 | 0.329933 | 1 |

## Cautious Interpretation

- No tested simple summary separated decisively from whole-stream mean compressed byte value across the full matrix. This keeps the M5a2 interpretation cautious and local.

## Re-land Comparison Note

- The same-scope re-land on current main changed materially from the previously reported unmerged M5a2 result: the closest tested simple neighbor was `lzmsa-mean-compressed-byte-value` rather than `lzmsa-suffix-third-mean-compressed-byte-value`, it beat whole-stream mean in 0 of 12 conditions rather than 7, and the best family was `whole-stream` rather than `coarse-positional`.

## Caveats

- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.
- The coarse summary family comparison is local to this synthetic benchmark and should not be overgeneralized.

## Artifact Notes

- Per-trial score rows: 31104
- Per-condition summary rows: 108
- ROC point rows: 28651
