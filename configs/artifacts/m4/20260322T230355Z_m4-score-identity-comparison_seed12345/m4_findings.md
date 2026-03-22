# M4 Score-Identity Comparison Findings

## Experimental Scope

- Tasks run: ofdm-signal-present-vs-noise-only, gaussian-emitter-vs-noise-only
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Trial count per condition and class: 24
- Detector identities compared: lzmsa-paper, lzmsa-compressed-length, lzmsa-normalized-compressed-length
- Seed: 12345
- Config provenance: m4-score-identity-comparison / M4 Synthetic Score-Identity Comparison

## Main Comparison Table

| Task | SNR dB | Window | AUC paper | AUC compressed length | AUC normalized length | Δ paper-length | Δ paper-normalized | Δ length-normalized |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gaussian-emitter-vs-noise-only | -9 | 64 | 0.604167 | 0.321181 | 0.321181 | 0.282986 | 0.282986 | 0.000000 |
| gaussian-emitter-vs-noise-only | -9 | 128 | 0.484375 | 0.429688 | 0.429688 | 0.054687 | 0.054687 | 0.000000 |
| gaussian-emitter-vs-noise-only | -3 | 64 | 0.456597 | 0.247396 | 0.247396 | 0.209201 | 0.209201 | 0.000000 |
| gaussian-emitter-vs-noise-only | -3 | 128 | 0.805556 | 0.111111 | 0.111111 | 0.694445 | 0.694445 | 0.000000 |
| gaussian-emitter-vs-noise-only | 0 | 64 | 0.657986 | 0.032118 | 0.032118 | 0.625868 | 0.625868 | 0.000000 |
| gaussian-emitter-vs-noise-only | 0 | 128 | 0.777778 | 0.010417 | 0.010417 | 0.767361 | 0.767361 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -9 | 64 | 0.438368 | 0.376736 | 0.376736 | 0.061632 | 0.061632 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -9 | 128 | 0.451389 | 0.421875 | 0.421875 | 0.029514 | 0.029514 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -3 | 64 | 0.684028 | 0.118924 | 0.118924 | 0.565104 | 0.565104 | 0.000000 |
| ofdm-signal-present-vs-noise-only | -3 | 128 | 0.564236 | 0.182292 | 0.182292 | 0.381944 | 0.381944 | 0.000000 |
| ofdm-signal-present-vs-noise-only | 0 | 64 | 0.487847 | 0.087674 | 0.087674 | 0.400173 | 0.400173 | 0.000000 |
| ofdm-signal-present-vs-noise-only | 0 | 128 | 0.645833 | 0.052951 | 0.052951 | 0.592882 | 0.592882 | 0.000000 |

## Short Findings

- Across 12 task/SNR/window conditions, the maximum pairwise AUC gap among the three compression-derived score identities was 0.767361, with an average per-condition worst-case gap of 0.388816.
- Across the tested synthetic sweep, `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` produced identical AUCs in every condition, so the observed mechanism split is between byte-sum scoring and compression-length-based scoring rather than between the two length-based variants.
- Within this synthetic benchmark, `lzmsa-paper` achieved the highest AUC in every tested condition rather than tracking closely with the length-based score identities.
- On gaussian-emitter-vs-noise-only, the average worst-case per-condition AUC gap was 0.439091; the largest gap appeared at SNR 0 dB and window length 128, where the worst pairwise difference reached 0.767361.
- On ofdm-signal-present-vs-noise-only, the average worst-case per-condition AUC gap was 0.338541; the largest gap appeared at SNR 0 dB and window length 128, where the worst pairwise difference reached 0.592882.
- Some synthetic conditions diverged materially under score substitution (threshold 0.05 AUC): gaussian-emitter-vs-noise-only @ -9 dB / window 64 (worst gap 0.282986); gaussian-emitter-vs-noise-only @ -9 dB / window 128 (worst gap 0.054687); gaussian-emitter-vs-noise-only @ -3 dB / window 64 (worst gap 0.209201).

## Caveats

- This M4 comparison is limited to the repository's synthetic benchmark tasks and conditions.
- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.
- The current deterministic serialization + Brotli compression backend remains fixed; M4 only varies score identity on top of that path.
- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.

## Artifact Notes

- Per-trial score rows: 1728
- Per-condition summary rows: 36
- ROC point rows: 1031
