# M4a Score-Identity Comparison Findings

## Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 72
- Detector identities compared: lzmsa-paper, lzmsa-compressed-length, lzmsa-normalized-compressed-length
- Seed: 24680
- Config provenance: m4a-score-identity-confirmation / M4a Synthetic Score-Identity Confirmation Rerun

## Stability Summary

- `lzmsa-paper` had the highest AUC in all tested conditions.
- `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` matched exactly in every tested condition.
- The largest paper-vs-length-based AUC gap was 0.690876 (large), and the median paper-vs-length-based AUC gap was 0.373554 (large).

## Comparison Table

| Task | SNR dB | Window | AUC paper | AUC compressed length | AUC normalized length | Δ paper-length | Δ paper-normalized | Δ length-normalized |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.490258 | 0.445313 | 0.445313 | 0.044945 | 0.044945 | 0.000000 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.486111 | 0.422068 | 0.422068 | 0.064043 | 0.064043 | 0.000000 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.520255 | 0.146701 | 0.146701 | 0.373554 | 0.373554 | 0.000000 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.488329 | 0.160783 | 0.160783 | 0.327546 | 0.327546 | 0.000000 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.581019 | 0.055459 | 0.055459 | 0.525560 | 0.525560 | 0.000000 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.691165 | 0.011285 | 0.011285 | 0.679880 | 0.679880 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.475116 | 0.350694 | 0.350694 | 0.124422 | 0.124422 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.614969 | 0.309124 | 0.309124 | 0.305845 | 0.305845 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.613812 | 0.276138 | 0.276138 | 0.337674 | 0.337674 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.640239 | 0.148823 | 0.148823 | 0.491416 | 0.491416 | 0.000000 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.609761 | 0.152874 | 0.152874 | 0.456887 | 0.456887 | 0.000000 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.707948 | 0.017072 | 0.017072 | 0.690876 | 0.690876 | 0.000000 |

## Cautious Conclusion

- The M4 ranking remained stable under the stronger M4a rerun: `lzmsa-paper` stayed on top throughout the tested synthetic matrix, while the two length-based variants remained interchangeable within the fixed-window conditions exercised here.

## Supporting Notes

- Across 12 task/SNR/window conditions, the maximum pairwise AUC gap among the three compression-derived score identities was 0.690876, with an average per-condition worst-case gap of 0.368554.
- Across the tested synthetic sweep, `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` produced identical AUCs in every condition, so the observed mechanism split is between byte-sum scoring and compression-length-based scoring rather than between the two length-based variants.
- Within this synthetic benchmark, `lzmsa-paper` achieved the highest AUC in every tested condition rather than tracking closely with the length-based score identities.
- On gaussian-emitter-vs-noise-only, the average worst-case per-condition AUC gap was 0.335921; the largest gap appeared at SNR 0 dB and window length 128, where the worst pairwise difference reached 0.679880.
- On ofdm-signal-present-vs-noise-only, the average worst-case per-condition AUC gap was 0.401187; the largest gap appeared at SNR 0 dB and window length 128, where the worst pairwise difference reached 0.690876.
- Some synthetic conditions diverged materially under score substitution (threshold 0.05 AUC): gaussian-emitter-vs-noise-only @ -9 dB / window 128 (worst gap 0.064043); gaussian-emitter-vs-noise-only @ -3 dB / window 64 (worst gap 0.373554); gaussian-emitter-vs-noise-only @ -3 dB / window 128 (worst gap 0.327546).
- M4a stays within the same scientific question as M4: same synthetic tasks, same serialization/compression path, same score formulas, and same ROC/AUC method.

## Caveats

- This M4a comparison is limited to the repository's synthetic benchmark tasks and conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed; M4 only varies score identity on top of that path.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.

## Artifact Notes

- Per-trial score rows: 5184
- Per-condition summary rows: 36
- ROC point rows: 2294
