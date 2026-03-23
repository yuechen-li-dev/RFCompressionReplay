# M5 Findings Freeze

## M5 Question

M5 asked two linked mechanism questions within the current synthetic harness:

- If `lzmsa-paper` is not mainly explained by compressed length, what compressed-stream property neighborhood is it actually tracking?
- How robust is that neighborhood to simple representation perturbations before compression?

## Scope

M5 was a synthetic-only mechanism-narrowing arc, not a full mechanism closure effort.

- Tasks covered:
  - `ofdm-signal-present-vs-noise-only`
  - `gaussian-emitter-vs-noise-only`
- M5a covered local compressed-stream decomposition under a fixed serialization + Brotli compression path.
- M5b covered representation perturbations while keeping the same synthetic tasks, compression backend, and focused feature neighborhood.
- All checked-in milestone runs follow the compact-artifact retention policy so the repo preserves auditable findings without retaining full raw exhaust by default.
- The arc narrows which compressed-byte summaries remain plausible local explanations for `lzmsa-paper`; it does not claim that the full detector mechanism is solved.

## Milestone-by-Milestone Summary

### M5a1

First local compressed-stream decomposition pass. Within the checked-in synthetic run, mean compressed byte value tracked `lzmsa-paper` more closely than compressed length did, weakening the clean compressed-length explanation. Artifacts: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/`.

### M5a2r

Compact re-land of the second decomposition pass under current milestone retention. The regenerated current-main result changed relative to the earlier unmerged run: `lzmsa-compressed-byte-bucket-64-127-proportion` was the closest tested simple neighbor by median absolute AUC delta, but the gap was not strong enough to treat that feature as a settled winner. Artifacts: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/`.

### M5a3

Stability pass over the current M5a2 feature family using a small explicit seed panel and higher trial counts. No single feature remained a clearly stable winner; the only stable conclusion was that the nearest-neighbor set stayed inside a coarse compressed-byte value / histogram / positional neighborhood. Artifacts: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/`.

### M5b1

First representation-perturbation exploration pass over a focused neighborhood panel. Modest perturbations reshuffled the local winner, but the neighborhood itself did not collapse outside the existing whole-stream / histogram / positional panel. Artifacts: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/`.

### M5b2

Axis-refinement pass that separated scale-only from packing-only perturbations. Both axes reshuffled the local winner relative to baseline, scale appeared to matter more than packing, and the same coarse neighborhood still remained intact. Artifacts: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/`.

### M5b3

Scale-handling refinement across a compact scale panel plus one normalization comparison. Raw scaling still reshuffled the local winner, but per-window RMS normalization reduced winner reshuffling; under that RMS-normalized handling, mean compressed byte value became the most stable practical simple summary across the tested scale panel while the broader coarse compressed-byte neighborhood remained intact. Artifacts: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/`.

## Supported Conclusion

Within the current synthetic harness, `lzmsa-paper` is better understood as tracking a scale-sensitive coarse compressed-byte value / distribution / position neighborhood rather than compressed length alone. Simple compressed-length metrics are insufficient. Under simple per-window RMS normalization, mean compressed byte value becomes the most stable practical simple summary across the tested scale panel.

## Non-Conclusions

M5 did not show any of the following:

- that the original paper's private dataset is explained;
- that an SDR-facing result has been established;
- that the true full detector mechanism is known;
- that mean compressed byte value is the exact mechanism rather than the current best practical simple proxy;
- that the synthetic OFDM-like task has LTE fidelity; or
- that the current deterministic compression-backend caveat has disappeared.

## Current Best Interpretation

The clean compressibility story is weakened by the checked-in M5 arc. The durable finding is not a single invariant feature winner, but a family-level picture: `lzmsa-paper` stays closest to coarse compressed-byte value / distribution / position structure, and the exact nearest simple summary is sensitive to how the representation handles scale. Scale handling is therefore an important confounder in the current synthetic harness. Within the tested panel, per-window RMS normalization stabilizes the proxy picture enough that mean compressed byte value becomes the strongest practical simple summary, while still sitting inside the broader coarse compressed-byte neighborhood rather than replacing it with a full mechanism theory.

## Open Question / Next Step

The next natural question is whether this same coarse compressed-byte neighborhood persists when the synthetic structure is broadened beyond the current two-task benchmark family. If that neighborhood continues to hold under more diverse synthetic structure, a later SDR-facing follow-up would become better motivated; M5 itself does not yet justify that step.

## Compact M5 Status Table

| Milestone | Question | Key result | Current status / implication | Artifact path |
| --- | --- | --- | --- | --- |
| M5a1 | Length vs mean byte value? | Mean compressed byte value tracked `lzmsa-paper` more closely than compressed length in the checked-in run. | Clean compressed-length story weakened. | `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/` |
| M5a2r | Which coarse summary is closest on current main? | Regenerated re-land pointed to `bucket-64-127`, but the result changed from the earlier unmerged run and was not decisive enough to crown a stable winner. | Winner instability became explicit. | `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/` |
| M5a3 | Does the M5a2r winner stay stable across seeds? | No stable single-feature winner across the tested seed panel. | Stable conclusion only at the coarse neighborhood level. | `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/` |
| M5b1 | Does modest representation perturbation collapse the neighborhood? | Winner reshuffled, but the neighborhood stayed inside the existing whole-stream / histogram / positional panel. | Neighborhood looked robust to modest perturbation. | `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/` |
| M5b2 | Which perturbation axis matters more? | Both scale-only and packing-only reshuffled the winner; scale appeared to matter more. | Scale sensitivity became the leading confounder. | `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/` |
| M5b3 | Can scale handling be made more stable? | Raw scaling still reshuffled winners, but RMS normalization reduced reshuffling and made mean compressed byte value the most stable practical simple summary across the tested scale panel. | Current best practical proxy identified, still within the broader neighborhood story. | `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/` |

## Compact Artifact Pointers

### M5a1

- Findings: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/m5a1_findings.md`
- Comparison CSV: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/m5a1_auc_comparison.csv`
- Delta summary: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/m5a1_delta_summary.csv`
- Manifest: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/manifest.json`

### M5a2r

- Findings: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/m5a2_findings.md`
- Comparison CSV: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/m5a2_auc_comparison.csv`
- Delta summary: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/m5a2_delta_summary.csv`
- Manifest: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/manifest.json`

### M5a3

- Findings: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/m5a3_findings.md`
- Comparison CSV: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/m5a3_auc_comparison.csv`
- Stability summary: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/m5a3_stability_summary.csv`
- Delta summary: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/m5a3_delta_summary.csv`
- Manifest: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/manifest.json`

### M5b1

- Findings: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/m5b1_findings.md`
- Comparison CSV: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/m5b1_auc_comparison.csv`
- Stability summary: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/m5b1_perturbation_stability_summary.csv`
- Delta summary: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/m5b1_delta_summary.csv`
- Manifest: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/manifest.json`

### M5b2

- Findings: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/m5b2_findings.md`
- Comparison CSV: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/m5b2_auc_comparison.csv`
- Axis summary: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/m5b2_axis_summary.csv`
- Delta summary: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/m5b2_delta_summary.csv`
- Manifest: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/manifest.json`

### M5b3

- Findings: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/m5b3_findings.md`
- Comparison CSV: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/m5b3_auc_comparison.csv`
- Scale summary: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/m5b3_scale_summary.csv`
- Delta summary: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/m5b3_delta_summary.csv`
- Manifest: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/manifest.json`
