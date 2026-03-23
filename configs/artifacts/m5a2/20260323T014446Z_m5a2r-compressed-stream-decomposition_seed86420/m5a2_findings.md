# M5a2 Compressed-Stream Decomposition Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 72
- Detector identities compared: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value
- Seed: 86420
- Config provenance: m5a2r-compressed-stream-decomposition / M5a2r Synthetic Compressed-Stream Decomposition

## Main Comparison Statement

- Within the current synthetic benchmark, `lzmsa-compressed-byte-bucket-64-127-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta.

## Condition Summary

- `lzmsa-compressed-byte-bucket-64-127-proportion` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta (0.039689).
- It was closer to `lzmsa-paper` than whole-stream mean compressed byte value in 6 of 12 tested conditions.
- The most informative tested feature family by best-member median absolute AUC delta was `coarse-histogram`.

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC mean | AUC variance | AUC bucket 0-63 | AUC bucket 64-127 | AUC bucket 128-191 | AUC bucket 192-255 | AUC prefix-third mean | AUC suffix-third mean | Δ paper-mean | Δ paper-variance | Δ paper-bucket 0-63 | Δ paper-bucket 64-127 | Δ paper-bucket 128-191 | Δ paper-bucket 192-255 | Δ paper-prefix-third | Δ paper-suffix-third |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.508681 | 0.462191 | 0.512731 | 0.561150 | 0.542631 | 0.422164 | 0.503279 | 0.578511 | 0.409722 | 0.046490 | -0.004050 | -0.052469 | -0.033950 | 0.086517 | 0.005402 | -0.069830 | 0.098959 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.498650 | 0.493441 | 0.462770 | 0.481578 | 0.518519 | 0.485436 | 0.499325 | 0.496528 | 0.496142 | 0.005209 | 0.035880 | 0.017072 | -0.019869 | 0.013214 | -0.000675 | 0.002122 | 0.002508 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.649884 | 0.597222 | 0.402199 | 0.390625 | 0.517072 | 0.565490 | 0.517843 | 0.587770 | 0.624421 | 0.052662 | 0.247685 | 0.259259 | 0.132812 | 0.084394 | 0.132041 | 0.062114 | 0.025463 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.588927 | 0.496142 | 0.533565 | 0.503858 | 0.521991 | 0.430556 | 0.546007 | 0.487269 | 0.490548 | 0.092785 | 0.055362 | 0.085069 | 0.066936 | 0.158371 | 0.042920 | 0.101658 | 0.098379 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.585745 | 0.471836 | 0.375000 | 0.486304 | 0.569927 | 0.570023 | 0.399402 | 0.465856 | 0.520833 | 0.113909 | 0.210745 | 0.099441 | 0.015818 | 0.015722 | 0.186343 | 0.119889 | 0.064912 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.755015 | 0.635224 | 0.391975 | 0.361979 | 0.575424 | 0.453125 | 0.593750 | 0.575810 | 0.610340 | 0.119791 | 0.363040 | 0.393036 | 0.179591 | 0.301890 | 0.161265 | 0.179205 | 0.144675 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.440297 | 0.425926 | 0.435185 | 0.564236 | 0.470197 | 0.564718 | 0.394772 | 0.446181 | 0.380015 | 0.014371 | 0.005112 | -0.123939 | -0.029900 | -0.124421 | 0.045525 | -0.005884 | 0.060282 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.547936 | 0.552469 | 0.521026 | 0.454186 | 0.480613 | 0.521316 | 0.543403 | 0.593943 | 0.397859 | -0.004533 | 0.026910 | 0.093750 | 0.067323 | 0.026620 | 0.004533 | -0.046007 | 0.150077 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.508681 | 0.458526 | 0.498264 | 0.583526 | 0.496721 | 0.460359 | 0.464988 | 0.441551 | 0.445602 | 0.050155 | 0.010417 | -0.074845 | 0.011960 | 0.048322 | 0.043693 | 0.067130 | 0.063079 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.575617 | 0.483025 | 0.386960 | 0.434799 | 0.598669 | 0.545042 | 0.426794 | 0.558449 | 0.441744 | 0.092592 | 0.188657 | 0.140818 | -0.023052 | 0.030575 | 0.148823 | 0.017168 | 0.133873 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.567901 | 0.490934 | 0.353202 | 0.476948 | 0.522473 | 0.525945 | 0.440201 | 0.541474 | 0.491609 | 0.076967 | 0.214699 | 0.090953 | 0.045428 | 0.041956 | 0.127700 | 0.026427 | 0.076292 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.663484 | 0.531057 | 0.488233 | 0.458719 | 0.519290 | 0.517747 | 0.526524 | 0.542631 | 0.528356 | 0.132427 | 0.175251 | 0.204765 | 0.144194 | 0.145737 | 0.136960 | 0.120853 | 0.135128 |

## Aggregate Delta Summary

| Alternative detector | Family | Median | Max | Conditions closer than whole-stream mean |
| --- | --- | ---: | ---: | ---: |
| lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.039689 | 0.179591 | 6 |
| lzmsa-prefix-third-mean-compressed-byte-value | coarse-positional | 0.064622 | 0.179205 | 5 |
| lzmsa-mean-compressed-byte-value | whole-stream | 0.064815 | 0.132427 | 0 |
| lzmsa-compressed-byte-bucket-128-191-proportion | coarse-histogram | 0.066358 | 0.301890 | 4 |
| lzmsa-compressed-byte-bucket-192-255-proportion | coarse-histogram | 0.086613 | 0.186343 | 4 |
| lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.087336 | 0.150077 | 4 |
| lzmsa-compressed-byte-bucket-0-63-proportion | coarse-histogram | 0.096596 | 0.393036 | 2 |
| lzmsa-compressed-byte-variance | whole-stream | 0.115306 | 0.363040 | 4 |

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

- Per-trial score rows: 15552
- Per-condition summary rows: 108
- ROC point rows: 14950
