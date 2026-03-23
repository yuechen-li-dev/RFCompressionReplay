# M5a2 Compressed-Stream Decomposition Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 144
- Detector identities compared: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value
- Seed: 24680
- Config provenance: m5a3-stability-confirmation / M5a3 Synthetic Stability Confirmation

## Main Comparison Statement

- Within the current synthetic benchmark, `lzmsa-compressed-byte-bucket-128-191-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta.

## Condition Summary

- `lzmsa-compressed-byte-bucket-128-191-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta (0.066021).
- It was closer to `lzmsa-paper` than whole-stream mean compressed byte value in 4 of 12 tested conditions.
- The most informative tested feature family by best-member median absolute AUC delta was `coarse-histogram`.

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC mean | AUC variance | AUC bucket 0-63 | AUC bucket 64-127 | AUC bucket 128-191 | AUC bucket 192-255 | AUC prefix-third mean | AUC suffix-third mean | Δ paper-mean | Δ paper-variance | Δ paper-bucket 0-63 | Δ paper-bucket 64-127 | Δ paper-bucket 128-191 | Δ paper-bucket 192-255 | Δ paper-prefix-third | Δ paper-suffix-third |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.506197 | 0.500675 | 0.507861 | 0.480421 | 0.539931 | 0.456115 | 0.494960 | 0.483049 | 0.567805 | 0.005522 | -0.001664 | 0.025776 | -0.033734 | 0.050082 | 0.011237 | 0.023148 | -0.061608 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.509983 | 0.494623 | 0.517409 | 0.530985 | 0.470414 | 0.490861 | 0.502990 | 0.472222 | 0.531684 | 0.015360 | -0.007426 | -0.021002 | 0.039569 | 0.019122 | 0.006993 | 0.037761 | -0.021701 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.522352 | 0.436728 | 0.497396 | 0.553916 | 0.513648 | 0.506149 | 0.418885 | 0.444517 | 0.524113 | 0.085624 | 0.024956 | -0.031564 | 0.008704 | 0.016203 | 0.103467 | 0.077835 | -0.001761 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.560595 | 0.486497 | 0.457803 | 0.513527 | 0.489415 | 0.517650 | 0.463445 | 0.504171 | 0.445698 | 0.074098 | 0.102792 | 0.047068 | 0.071180 | 0.042945 | 0.097150 | 0.056424 | 0.114897 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.644218 | 0.529900 | 0.438850 | 0.450569 | 0.571181 | 0.498963 | 0.462842 | 0.533275 | 0.498650 | 0.114318 | 0.205368 | 0.193649 | 0.073037 | 0.145255 | 0.181376 | 0.110943 | 0.145568 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.719305 | 0.583623 | 0.413387 | 0.435692 | 0.510802 | 0.522835 | 0.543210 | 0.513985 | 0.576726 | 0.135682 | 0.305918 | 0.283613 | 0.208503 | 0.196470 | 0.176095 | 0.205320 | 0.142579 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.457779 | 0.427324 | 0.512539 | 0.575448 | 0.539207 | 0.461637 | 0.439646 | 0.450328 | 0.429905 | 0.030455 | -0.054760 | -0.117669 | -0.081428 | -0.003858 | 0.018133 | 0.007451 | 0.027874 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.517409 | 0.490500 | 0.532022 | 0.523968 | 0.499132 | 0.455681 | 0.535494 | 0.508922 | 0.470824 | 0.026909 | -0.014613 | -0.006559 | 0.018277 | 0.061728 | -0.018085 | 0.008487 | 0.046585 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.569493 | 0.537953 | 0.466387 | 0.450593 | 0.522786 | 0.488691 | 0.537254 | 0.536217 | 0.472126 | 0.031540 | 0.103106 | 0.118900 | 0.046707 | 0.080802 | 0.032239 | 0.033276 | 0.097367 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.635441 | 0.566840 | 0.431424 | 0.423442 | 0.520761 | 0.537471 | 0.511791 | 0.514709 | 0.527705 | 0.068601 | 0.204017 | 0.211999 | 0.114680 | 0.097970 | 0.123650 | 0.120732 | 0.107736 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.613137 | 0.530213 | 0.386333 | 0.416305 | 0.545693 | 0.542824 | 0.500482 | 0.526910 | 0.535229 | 0.082924 | 0.226804 | 0.196832 | 0.067444 | 0.070313 | 0.112655 | 0.086227 | 0.077908 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.706404 | 0.595486 | 0.509066 | 0.421923 | 0.534216 | 0.472512 | 0.587432 | 0.513214 | 0.572868 | 0.110918 | 0.197338 | 0.284481 | 0.172188 | 0.233892 | 0.118972 | 0.193190 | 0.133536 |

## Aggregate Delta Summary

| Alternative detector | Family | Median | Max | Conditions closer than whole-stream mean |
| --- | --- | ---: | ---: | ---: |
| lzmsa-compressed-byte-bucket-128-191-proportion | coarse-histogram | 0.066021 | 0.233892 | 4 |
| lzmsa-prefix-third-mean-compressed-byte-value | coarse-positional | 0.067130 | 0.205320 | 5 |
| lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.069312 | 0.208503 | 5 |
| lzmsa-mean-compressed-byte-value | whole-stream | 0.071350 | 0.135682 | 0 |
| lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.087638 | 0.145568 | 3 |
| lzmsa-compressed-byte-bucket-192-255-proportion | coarse-histogram | 0.100309 | 0.181376 | 3 |
| lzmsa-compressed-byte-variance | whole-stream | 0.102949 | 0.305918 | 4 |
| lzmsa-compressed-byte-bucket-0-63-proportion | coarse-histogram | 0.118285 | 0.284481 | 3 |

## Cautious Interpretation

- No tested simple summary separated decisively from whole-stream mean compressed byte value across the full matrix. This keeps the M5a2 interpretation cautious and local.

## Re-land Comparison Note

- The same-scope re-land on current main changed materially from the previously reported unmerged M5a2 result: the closest tested simple neighbor was `lzmsa-compressed-byte-bucket-128-191-proportion` rather than `lzmsa-suffix-third-mean-compressed-byte-value`, it beat whole-stream mean in 4 of 12 conditions rather than 7, and the best family was `coarse-histogram` rather than `coarse-positional`.

## Caveats

- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.
- The coarse summary family comparison is local to this synthetic benchmark and should not be overgeneralized.

## Artifact Notes

- Per-trial score rows: 31104
- Per-condition summary rows: 108
- ROC point rows: 28631
