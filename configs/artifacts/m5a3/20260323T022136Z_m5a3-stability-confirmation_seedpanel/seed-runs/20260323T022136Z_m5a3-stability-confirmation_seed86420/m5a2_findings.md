# M5a2 Compressed-Stream Decomposition Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 144
- Detector identities compared: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value
- Seed: 86420
- Config provenance: m5a3-stability-confirmation / M5a3 Synthetic Stability Confirmation

## Main Comparison Statement

- Within the current synthetic benchmark, `lzmsa-compressed-byte-bucket-64-127-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta.

## Condition Summary

- `lzmsa-compressed-byte-bucket-64-127-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta (0.044862).
- It was closer to `lzmsa-paper` than whole-stream mean compressed byte value in 6 of 12 tested conditions.
- The most informative tested feature family by best-member median absolute AUC delta was `coarse-histogram`.

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC mean | AUC variance | AUC bucket 0-63 | AUC bucket 64-127 | AUC bucket 128-191 | AUC bucket 192-255 | AUC prefix-third mean | AUC suffix-third mean | Δ paper-mean | Δ paper-variance | Δ paper-bucket 0-63 | Δ paper-bucket 64-127 | Δ paper-bucket 128-191 | Δ paper-bucket 192-255 | Δ paper-prefix-third | Δ paper-suffix-third |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.535132 | 0.482976 | 0.504244 | 0.528236 | 0.539183 | 0.414472 | 0.536000 | 0.541160 | 0.453270 | 0.052156 | 0.030888 | 0.006896 | -0.004051 | 0.120660 | -0.000868 | -0.006028 | 0.081862 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.471137 | 0.478057 | 0.501013 | 0.510176 | 0.512659 | 0.475043 | 0.469425 | 0.494261 | 0.447049 | -0.006920 | -0.029876 | -0.039039 | -0.041522 | -0.003906 | 0.001712 | -0.023124 | 0.024088 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.639660 | 0.579524 | 0.415509 | 0.402079 | 0.523413 | 0.532745 | 0.518398 | 0.584660 | 0.559221 | 0.060136 | 0.224151 | 0.237581 | 0.116247 | 0.106915 | 0.121262 | 0.055000 | 0.080439 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.580826 | 0.484423 | 0.493490 | 0.492525 | 0.538701 | 0.477262 | 0.505835 | 0.520399 | 0.438802 | 0.096403 | 0.087336 | 0.088301 | 0.042125 | 0.103564 | 0.074991 | 0.060427 | 0.142024 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.625000 | 0.515239 | 0.424142 | 0.492959 | 0.480830 | 0.568552 | 0.468268 | 0.503231 | 0.537833 | 0.109761 | 0.200858 | 0.132041 | 0.144170 | 0.056448 | 0.156732 | 0.121769 | 0.087167 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.748409 | 0.630932 | 0.408758 | 0.377966 | 0.539255 | 0.488860 | 0.588156 | 0.599465 | 0.576485 | 0.117477 | 0.339651 | 0.370443 | 0.209154 | 0.259549 | 0.160253 | 0.148944 | 0.171924 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.405985 | 0.396557 | 0.438609 | 0.587047 | 0.453583 | 0.581525 | 0.381510 | 0.436608 | 0.400825 | 0.009428 | -0.032624 | -0.181062 | -0.047598 | -0.175540 | 0.024475 | -0.030623 | 0.005160 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.547912 | 0.547743 | 0.512201 | 0.454210 | 0.473621 | 0.514564 | 0.557243 | 0.545380 | 0.497058 | 0.000169 | 0.035711 | 0.093702 | 0.074291 | 0.033348 | -0.009331 | 0.002532 | 0.050854 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.534698 | 0.473524 | 0.489149 | 0.537977 | 0.504895 | 0.462360 | 0.484688 | 0.485098 | 0.460359 | 0.061174 | 0.045549 | -0.003279 | 0.029803 | 0.072338 | 0.050010 | 0.049600 | 0.074339 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.591676 | 0.503014 | 0.389853 | 0.428916 | 0.601876 | 0.511381 | 0.466749 | 0.482060 | 0.540389 | 0.088662 | 0.201823 | 0.162760 | -0.010200 | 0.080295 | 0.124927 | 0.109616 | 0.051287 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.545645 | 0.473958 | 0.373987 | 0.497806 | 0.516614 | 0.505956 | 0.458647 | 0.525463 | 0.488812 | 0.071687 | 0.171658 | 0.047839 | 0.029031 | 0.039689 | 0.086998 | 0.020182 | 0.056833 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.670669 | 0.535542 | 0.480035 | 0.448423 | 0.538749 | 0.507258 | 0.525222 | 0.553193 | 0.494695 | 0.135127 | 0.190634 | 0.222246 | 0.131920 | 0.163411 | 0.145447 | 0.117476 | 0.175974 |

## Aggregate Delta Summary

| Alternative detector | Family | Median | Max | Conditions closer than whole-stream mean |
| --- | --- | ---: | ---: | ---: |
| lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.044862 | 0.209154 | 6 |
| lzmsa-prefix-third-mean-compressed-byte-value | coarse-positional | 0.052300 | 0.148944 | 6 |
| lzmsa-mean-compressed-byte-value | whole-stream | 0.066431 | 0.135127 | 0 |
| lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.077389 | 0.175974 | 4 |
| lzmsa-compressed-byte-bucket-192-255-proportion | coarse-histogram | 0.080995 | 0.160253 | 4 |
| lzmsa-compressed-byte-bucket-128-191-proportion | coarse-histogram | 0.091930 | 0.259549 | 4 |
| lzmsa-compressed-byte-bucket-0-63-proportion | coarse-histogram | 0.112871 | 0.370443 | 4 |
| lzmsa-compressed-byte-variance | whole-stream | 0.129497 | 0.339651 | 3 |

## Cautious Interpretation

- No tested simple summary separated decisively from whole-stream mean compressed byte value across the full matrix. This keeps the M5a2 interpretation cautious and local.

## Re-land Comparison Note

- The same-scope re-land on current main changed materially from the previously reported unmerged M5a2 result: the closest tested simple neighbor was `lzmsa-compressed-byte-bucket-64-127-proportion` rather than `lzmsa-suffix-third-mean-compressed-byte-value`, it beat whole-stream mean in 6 of 12 conditions rather than 7, and the best family was `coarse-histogram` rather than `coarse-positional`.

## Caveats

- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.
- The coarse summary family comparison is local to this synthetic benchmark and should not be overgeneralized.

## Artifact Notes

- Per-trial score rows: 31104
- Per-condition summary rows: 108
- ROC point rows: 28577
