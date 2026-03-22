# M4b Findings Freeze: Score-Identity Status

## Question

When the serialization and compression path are held fixed, how does detector behavior change when the score identity is:

- compressed byte sum (`lzmsa-paper`)
- compressed length (`lzmsa-compressed-length`)
- normalized compressed length (`lzmsa-normalized-compressed-length`)

## Scope

This findings freeze covers only the checked-in M4 and M4a synthetic comparison runs.

| Run | Run id | Scope | Tasks | SNR dB | Window lengths | Detectors | Per-class trials / condition | Key result |
| --- | --- | --- | --- | --- | --- | --- | ---: | --- |
| M4 | `20260322T230355Z_m4-score-identity-comparison_seed12345` | Synthetic-only benchmark; 12 task/SNR/window conditions | `ofdm-signal-present-vs-noise-only`; `gaussian-emitter-vs-noise-only` | `-9`, `-3`, `0` | `64`, `128` | `lzmsa-paper`; `lzmsa-compressed-length`; `lzmsa-normalized-compressed-length` | 24 | `lzmsa-paper` highest AUC in every tested condition; the two length-based detectors matched in every tested condition |
| M4a | `20260322T231911Z_m4a-score-identity-confirmation_seed24680` | Same synthetic-only benchmark; same 12 conditions | `ofdm-signal-present-vs-noise-only`; `gaussian-emitter-vs-noise-only` | `-9`, `-3`, `0` | `64`, `128` | `lzmsa-paper`; `lzmsa-compressed-length`; `lzmsa-normalized-compressed-length` | 72 | Same qualitative ranking under higher per-condition trial counts |

Artifact locations for the checked-in runs:

- M4 artifacts: `configs/artifacts/m4/20260322T230355Z_m4-score-identity-comparison_seed12345/`
  - comparison CSV: `configs/artifacts/m4/20260322T230355Z_m4-score-identity-comparison_seed12345/m4_auc_comparison.csv`
  - findings file: `configs/artifacts/m4/20260322T230355Z_m4-score-identity-comparison_seed12345/m4_findings.md`
  - manifest: `configs/artifacts/m4/20260322T230355Z_m4-score-identity-comparison_seed12345/manifest.json`
- M4a artifacts: `configs/artifacts/m4a/20260322T231911Z_m4a-score-identity-confirmation_seed24680/`
  - comparison CSV: `configs/artifacts/m4a/20260322T231911Z_m4a-score-identity-confirmation_seed24680/m4a_auc_comparison.csv`
  - findings file: `configs/artifacts/m4a/20260322T231911Z_m4a-score-identity-confirmation_seed24680/m4a_findings.md`
  - manifest: `configs/artifacts/m4a/20260322T231911Z_m4a-score-identity-confirmation_seed24680/manifest.json`

## Main Result

M4 established that, across the checked-in synthetic benchmark sweep, `lzmsa-paper` achieved the highest AUC in every tested condition, while `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` matched each other across the tested fixed-window conditions.

M4a reran the same question with higher per-condition trial counts and confirmed the same qualitative result: `lzmsa-paper` again had the highest AUC in every tested condition, and the two length-based detectors again matched each other across the tested fixed-window conditions.

Across both runs, the paper-vs-length AUC gaps remained materially nontrivial rather than collapsing under the higher-count rerun.

## Supported Conclusion

Within this independently regenerated synthetic benchmark, detector behavior was not robust to substituting true compression-length-based metrics for the paper-style byte-sum statistic.

Therefore, the clean “works because of compressibility” interpretation is weakened in the current synthetic harness.

## Non-conclusions

This findings freeze does **not** show any of the following:

- a claim about the original paper's private dataset
- an SDR or over-the-air result
- proof that the paper's detector is useless
- proof that the true mechanism is now known
- a claim of LTE fidelity
- a claim that the current Brotli-backed implementation is exact LZMA parity

## Current Best Interpretation

The current best mechanistic reading is that the paper-style statistic appears to derive materially different behavior from properties of the compressed output values, not from compressed length alone.

## Next Question

What property of the compressed bytestream is the byte-sum statistic actually keying on?
