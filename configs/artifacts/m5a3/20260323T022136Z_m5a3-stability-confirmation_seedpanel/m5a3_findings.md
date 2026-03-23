# M5a3 Stability Confirmation Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 144
- Seeds used: 86420, 97531, 24680
- Included features: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value
- Config provenance: m5a3-stability-confirmation / M5a3 Synthetic Stability Confirmation

## Stability Summary

- `lzmsa-mean-compressed-byte-value` led the current feature set, but not by enough to treat the winner as stable across the 36 seed-condition combinations.
- `lzmsa-mean-compressed-byte-value` had median absolute AUC delta 0.070144, max absolute AUC delta 0.135682, and median closeness rank 3.000000.
- The leading family by best-member stability metrics was `whole-stream`, but the strongest contenders spanned whole-stream, coarse-positional, coarse-histogram.

## Main Conclusion

- Within the current synthetic benchmark, no single M5a2 feature was a clearly stable winner; the nearest-neighbor set remained split across whole-stream, coarse-positional, coarse-histogram summaries rather than collapsing to one feature or one family.

## Stability Table

| Alternative detector | Family | Closest-neighbor wins | Median | Max | Median rank |
| --- | --- | ---: | ---: | ---: | ---: |
| lzmsa-mean-compressed-byte-value | whole-stream | 11 | 0.070144 | 0.135682 | 3.000000 |
| lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 6 | 0.077064 | 0.175974 | 3.000000 |
| lzmsa-compressed-byte-bucket-128-191-proportion | coarse-histogram | 5 | 0.080549 | 0.259549 | 4.000000 |
| lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 4 | 0.072108 | 0.209154 | 4.000000 |
| lzmsa-prefix-third-mean-compressed-byte-value | coarse-positional | 4 | 0.073821 | 0.205320 | 4.000000 |
| lzmsa-compressed-byte-bucket-192-255-proportion | coarse-histogram | 3 | 0.092074 | 0.181376 | 4.000000 |
| lzmsa-compressed-byte-bucket-0-63-proportion | coarse-histogram | 2 | 0.134079 | 0.370443 | 7.000000 |
| lzmsa-compressed-byte-variance | whole-stream | 1 | 0.106337 | 0.339651 | 7.000000 |

## Caveats

- This remains a synthetic-only benchmark; it does not establish broader real-world sensing behavior.
- The OFDM-like task is a structured synthetic proxy and is not LTE fidelity.
- The current deterministic serialization + Brotli compression backend caveat remains unchanged.
- No SDR capture, OTA, or hardware claims are supported by this artifact set.
- The bytestream mechanism is still not fully resolved; this milestone only checks winner stability within the current M5a2 feature family.
