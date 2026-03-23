# M5b3 Scale-Handling Refinement Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds used: 86420, 97531, 24680
- Trial count per condition and class: 72
- Scale values tested: 0.5x, 1x, 2x, 4x
- Normalization rule used: per-window RMS normalization to target RMS 1 before serialization for `normalized-rms`; `raw-scaled` uses no normalization.
- Feature panel used: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value
- Retention mode used: milestone (the repository's nearest compact retention mode for this exploration).
- Config provenance: m5b3-scale-handling-refinement / M5b3 Synthetic Scale-Handling Refinement

## Scale-Sensitivity Read

- Raw-scaled scale-trend winners by median absolute AUC delta: 0.5x -> `lzmsa-mean-compressed-byte-value`; 1x -> `lzmsa-compressed-byte-bucket-64-127-proportion`; 2x -> `lzmsa-mean-compressed-byte-value`; 4x -> `lzmsa-mean-compressed-byte-value`.
- RMS-normalized scale-trend winners by median absolute AUC delta: 0.5x -> `lzmsa-mean-compressed-byte-value`; 1x -> `lzmsa-mean-compressed-byte-value`; 2x -> `lzmsa-mean-compressed-byte-value`; 4x -> `lzmsa-mean-compressed-byte-value`.
- Raw-scaled pattern read: the winner changed across part of the scale panel but not every step, so this looks like localized reshuffling rather than a clean monotone handoff.
- RMS-normalized pattern read: the same winner held across all tested scales (`lzmsa-mean-compressed-byte-value`), so this looks stable rather than monotone-reshuffling.
- Overall raw-scaled nearest practical leader across the tested panel: `lzmsa-mean-compressed-byte-value` (whole-stream-mean, median absolute AUC delta 0.012636, closest-neighbor wins 92/144).
- Overall RMS-normalized nearest practical leader across the tested panel: `lzmsa-mean-compressed-byte-value` (whole-stream-mean, median absolute AUC delta 0.008343, closest-neighbor wins 132/144).

## Normalization Read

- Winner transitions across adjacent scales: raw-scaled = 2, normalized-rms = 0.
- Within this tested panel, the RMS-normalized variant reduced winner reshuffling relative to raw scaling.
- The normalization comparison materially changes the single-feature winner story only if the scale-leader sequence or overall family win counts move; inspect `m5b3_scale_summary.csv` for the exact counts and deltas.

## Family-Level Interpretation

- Across both representation families, the closest practical neighbors remained inside the same coarse compressed-byte value / distribution / position neighborhood established earlier; M5b3 refines scale handling rather than changing that family-level story.
- Within the tested scale panel, the raw-scaled overall leader was `lzmsa-mean-compressed-byte-value` and the normalized overall leader was `lzmsa-mean-compressed-byte-value`.

## Scale Trend Table

| Representation family | Scale | Median-delta leader | Family | Median | Max | Closest-win leader | Closest wins |
| --- | ---: | --- | --- | ---: | ---: | --- | ---: |
| raw-scaled | 0.5x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009934 | 0.029514 | lzmsa-mean-compressed-byte-value | 29/36 |
| raw-scaled | 1x | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0.066165 | 0.205343 | lzmsa-mean-compressed-byte-value | 14/36 |
| raw-scaled | 2x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.008247 | 0.045139 | lzmsa-mean-compressed-byte-value | 28/36 |
| raw-scaled | 4x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.009163 | 0.043596 | lzmsa-mean-compressed-byte-value | 21/36 |
| normalized-rms | 0.5x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.008343 | 0.027199 | lzmsa-mean-compressed-byte-value | 33/36 |
| normalized-rms | 1x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.008343 | 0.027199 | lzmsa-mean-compressed-byte-value | 33/36 |
| normalized-rms | 2x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.008343 | 0.027199 | lzmsa-mean-compressed-byte-value | 33/36 |
| normalized-rms | 4x | lzmsa-mean-compressed-byte-value | whole-stream-mean | 0.008343 | 0.027199 | lzmsa-mean-compressed-byte-value | 33/36 |

## Overall Delta Summary by Representation Family

| Representation family | Feature | Family | Closest wins | Median | Max | Median rank |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| normalized-rms | lzmsa-mean-compressed-byte-value | whole-stream-mean | 132/144 | 0.008343 | 0.027199 | 1.000000 |
| normalized-rms | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 12/144 | 0.034047 | 0.110725 | 2.000000 |
| normalized-rms | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 0/144 | 0.070843 | 0.233989 | 3.000000 |
| raw-scaled | lzmsa-mean-compressed-byte-value | whole-stream-mean | 92/144 | 0.012636 | 0.138503 | 1.000000 |
| raw-scaled | lzmsa-suffix-third-mean-compressed-byte-value | coarse-positional | 29/144 | 0.045091 | 0.156057 | 2.000000 |
| raw-scaled | lzmsa-compressed-byte-bucket-64-127-proportion | coarse-histogram | 23/144 | 0.058739 | 0.239680 | 3.000000 |

## Caveats

- This remains a synthetic-only benchmark and is not an SDR, OTA, or deployment claim.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.
- The deterministic Brotli compression backend caveat remains unchanged.
- No SDR capture or hardware claims are supported here.
- M5b3 is exploratory and compact-summary-first; it refines scale handling but does not resolve mechanism identity beyond the tested neighborhood.
- The normalization comparison uses one simple per-window RMS rule only; no broader normalization family sweep was attempted.

## Artifact Notes

- Comparison combinations retained: 288
- Delta summary rows retained: 6
- Scale summary rows retained: 24
