# M5b2 Perturbation-Axis Refinement Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds used: 86420, 97531, 24680
- Trial count per condition and class: 72
- Perturbations used: baseline [baseline] = Baseline representation: sampleScale 1.0 with float64-le serialization.; scale-half [scale] = Scale-only perturbation: multiply each sample by 0.5 before float64-le serialization. No extra clipping or normalization is applied beyond the raw scaled IEEE-754 value.; float32 [packing] = Packing-only perturbation: keep baseline scale but cast each sample to float32 before little-endian serialization. No extra clipping is applied beyond the float cast.; scale-half-float32 [combined] = Combined perturbation: multiply each sample by 0.5, then cast to float32 before little-endian serialization. No extra clipping or normalization is applied beyond the float cast of the scaled value.
- Feature panel used: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value
- Retention mode used: milestone (the repository's nearest compact retention mode for this exploration). 
- Config provenance: m5b2-perturbation-axis-refinement / M5b2 Synthetic Perturbation-Axis Refinement

## Axis-Level Read

- Baseline median-delta leader: `lzmsa-compressed-byte-bucket-64-127-proportion` (coarse-histogram, median absolute AUC delta 0.066165, closest-neighbor wins 14/36 for `lzmsa-mean-compressed-byte-value`).
- Scale-only median-delta leader: `lzmsa-mean-compressed-byte-value` (whole-stream-mean, median absolute AUC delta 0.009934); closest-neighbor win leader: `lzmsa-mean-compressed-byte-value` with 29/36 combinations.
- Packing-only median-delta leader: `lzmsa-mean-compressed-byte-value` (whole-stream-mean, median absolute AUC delta 0.020736); closest-neighbor win leader: `lzmsa-mean-compressed-byte-value` with 20/36 combinations.
- Combined perturbation median-delta leader: `lzmsa-mean-compressed-byte-value` (whole-stream-mean, median absolute AUC delta 0.020159); closest-neighbor win leader: `lzmsa-mean-compressed-byte-value` with 19/36 combinations.
- Scale-only reshuffle relative to baseline: yes. Packing-only reshuffle relative to baseline: yes.
- Within the tested perturbations, both axes mattered, but the scale-only split moved the median-delta winner farther from the baseline summary than the packing-only split.

## Axis Comparison Table

| Axis | Median-delta leader | Family | Median | Max | Closest-win leader | Closest wins | 
| --- | --- | --- | ---: | ---: | --- | ---: |
| baseline | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.066165 | 0.205343 | lzmsa-mean-compressed-byte-value | 14/36 |
| scale | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009934 | 0.029514 | lzmsa-mean-compressed-byte-value | 29/36 |
| packing | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020736 | 0.055556 | lzmsa-mean-compressed-byte-value | 20/36 |
| combined | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020159 | 0.076196 | lzmsa-mean-compressed-byte-value | 19/36 |

## Family-Level Interpretation

- Across the tested axis split, the single-feature winner moved, but all axis leaders stayed inside the existing coarse compressed-byte neighborhood established in M5a/M5b1.
- The neighborhood remained robust at the family level while the single-feature winner stayed axis-sensitive inside the tested whole-stream / histogram / positional panel.

## Delta Summary by Perturbation

| Perturbation | Axis | Feature | Family | Median | Max |
| --- | --- | --- | --- | ---: | ---: |
| baseline | baseline | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.066165 | 0.205343 |
| baseline | baseline | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.069444 | 0.138503 |
| baseline | baseline | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.075859 | 0.156057 |
| scale-half | scale | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009934 | 0.029514 |
| scale-half | scale | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.034192 | 0.139564 |
| scale-half | scale | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.050492 | 0.230806 |
| float32 | packing | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020736 | 0.055556 |
| float32 | packing | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.028743 | 0.126737 |
| float32 | packing | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.055169 | 0.341628 |
| scale-half-float32 | combined | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.020159 | 0.076196 |
| scale-half-float32 | combined | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 0.039834 | 0.089410 |
| scale-half-float32 | combined | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.046586 | 0.247299 |

## Caveats

- This remains a synthetic-only benchmark and is not an SDR, OTA, or deployment claim.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.
- The deterministic Brotli compression backend caveat remains unchanged.
- No SDR capture or hardware claims are supported here.
- M5b2 is exploratory and compact-summary-first; it separates perturbation axes but does not resolve mechanism identity beyond the tested neighborhood.
- The scale perturbation uses a deterministic multiplicative factor before serialization with no extra clipping or normalization beyond the selected IEEE float cast.

## Artifact Notes

- Comparison combinations retained: 144
- Delta summary rows retained: 12
- Axis summary rows retained: 12
