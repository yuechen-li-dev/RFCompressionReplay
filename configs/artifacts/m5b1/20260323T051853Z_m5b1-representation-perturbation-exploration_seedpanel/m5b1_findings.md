# M5b1 Representation Perturbation Exploration Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds used: 86420, 97531, 24680
- Trial count per condition and class: 72
- Perturbations used: baseline = Baseline representation: sampleScale 1.0 with float64-le serialization.; scale-half = Numeric scaling perturbation: multiply each sample by 0.5 before float64-le serialization. No extra clipping or normalization is applied beyond the raw scaled IEEE-754 value.; float32 = Serialization perturbation: keep baseline scale but cast each sample to float32 before little-endian serialization. No extra clipping is applied beyond the float cast.
- Feature panel used: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value
- Representative feature choice note: `lzmsa-compressed-byte-bucket-64-127-proportion` is the histogram representative because the checked-in M5a2r compact run reported it as the closest simple neighbor, while `lzmsa-suffix-third-mean-compressed-byte-value` is the positional representative because prior M5a2/M5a3 evidence kept the suffix-third family in the nearest-neighbor set.
- Retention mode used: milestone (used as the nearest compact retention mode already supported by the repository policy).
- Config provenance: m5b1-representation-perturbation-exploration / M5b1 Synthetic Representation Perturbation Exploration

## Main Perturbation Read

- Within the tested perturbations, the single-feature median-delta leader changed, but the tested nearest-neighbor set did not collapse outside the existing M5a neighborhood.
- Overall closest tested feature by closest-neighbor wins was `lzmsa-mean-compressed-byte-value` (whole-stream-mean), with 63 wins, median absolute AUC delta 0.019146, and max absolute AUC delta 0.138503.
- By per-perturbation median absolute AUC delta, the leaders were: baseline -> lzmsa-compressed-byte-bucket-64-127-proportion (coarse-histogram, median 0.066165); scale-half -> lzmsa-mean-compressed-byte-value (whole-stream-mean, median 0.009934); float32 -> lzmsa-mean-compressed-byte-value (whole-stream-mean, median 0.020736).

## Per-Perturbation Read

| Perturbation | Best median-delta feature | Family | Median | Max | Closest-neighbor wins |
| --- | --- | --- | ---: | ---: | ---: |
| baseline | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.066165 | 0.205343 | 11 (closest-win leader: lzmsa-mean-compressed-byte-value) |
| scale-half | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009934 | 0.029514 | 29  |
| float32 | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020736 | 0.055556 | 20  |

## Family-Level Interpretation

- The checked perturbation set moved the overall closest-neighbor counts toward whole-stream mean compressed byte value, while the baseline median-delta view still kept the current histogram representative competitive rather than collapsing the neighborhood entirely.
- Whole-stream mean representative (`lzmsa-mean-compressed-byte-value`) overall median absolute AUC delta: 0.019146.
- Histogram representative (`lzmsa-compressed-byte-bucket-64-127-proportion`) overall median absolute AUC delta: 0.057775.
- Positional representative (`lzmsa-suffix-third-mean-compressed-byte-value`) overall median absolute AUC delta: 0.035397.

## Delta Summary by Perturbation

| Perturbation | Feature | Family | Median | Max |
| --- | --- | --- | ---: | ---: |
| baseline | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.066165 | 0.205343 |
| baseline | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.069444 | 0.138503 |
| baseline | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.075859 | 0.156057 |
| float32 | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020736 | 0.055556 |
| float32 | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.028743 | 0.126737 |
| float32 | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.055169 | 0.341628 |
| scale-half | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009934 | 0.029514 |
| scale-half | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.034192 | 0.139564 |
| scale-half | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.050492 | 0.230806 |

## Caveats

- This is still a synthetic-only benchmark and should not be read as an SDR or deployment claim.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.
- The deterministic Brotli compression backend caveat remains unchanged.
- No SDR capture, OTA, or hardware claims are supported here.
- M5b1 is exploratory and compact-summary-first; it checks modest representation robustness only, not full mechanism resolution.
- The numeric scaling perturbation applies a deterministic multiplicative factor before serialization, with no extra clipping or normalization beyond the selected IEEE float cast.

## Artifact Notes

- Comparison combinations retained: 108
- Delta summary rows retained: 9
- Stability summary rows retained: 12
