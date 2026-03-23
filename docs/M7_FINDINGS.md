# M7 Findings Freeze

## M7 Question

M7 followed the M6 usefulness-mapping freeze because M6 did not support a detector-replacement story for the compression-derived feature family. The next question was narrower and more applied: if the family is not a strong replacement detector, does it fit better as a stream segmentation / change-point helper, and does simple boundary fusion make that helper role practically useful?

## Scope

M7 covered:

- a synthetic-only stream-level benchmark,
- M7b change-point / segmentation usefulness mapping across three deterministic transition families,
- M7b2 complementary boundary fusion on that same stream suite,
- compact summary-first artifacts intended to be easy to inspect without retaining large trace dumps.

M7 was an applied role exploration pass, not deployment validation, not an SDR result, and not a claim about the original paper's private data.

## Milestone-by-Milestone Summary

### M7b

M7b tested whether `lzmsa-rms-normalized-mean-compressed-byte-value` provided useful boundary information on three stream families: `quiet-to-structured-regime`, `correlated-nuisance-to-engineered-structure`, and `structure-to-structure-regime-shift`, against the compact baseline panel `ed` and `cav`. In the checked-in run, ED remained strongest on `quiet-to-structured-regime`; RMS-normalized mean remained secondary overall, but it was competitive on the correlated-nuisance and structure-shift families and recorded four distinct onset-hit conditions that the best baseline missed. That pattern fit a cautious segmentation/helper framing better than a replacement-detector framing.

Relevant compact artifacts:

- findings: `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/m7b_findings.md`
- comparison CSV: `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/m7b_boundary_comparison.csv`
- task summary: `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/m7b_task_summary.csv`
- manifest: `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/manifest.json`

### M7b2

M7b2 tested one simple normalized-average fusion of the ED, CAV, and RMS-normalized mean adjacent-change traces over the unchanged M7b stream suite. In the checked-in run, the fused signal was best or tied-best on 23 of 54 evaluated conditions, but the median onset-hit lift over the best single signal was 0.000 in all three families. The follow-up still recorded modest recovery cases, including one `quiet-to-structured-regime` condition where the fusion hit while all three standalone signals missed. That supports occasional complementary boundary information, but not a strong aggregate gain from this simple fusion rule.

Relevant compact artifacts:

- findings: `configs/artifacts/m7b2/20260323T093359Z_m7b2-complementary-boundary-fusion_seedpanel/m7b2_findings.md`
- comparison CSV: `configs/artifacts/m7b2/20260323T093359Z_m7b2-complementary-boundary-fusion_seedpanel/m7b2_boundary_comparison.csv`
- fusion summary: `configs/artifacts/m7b2/20260323T093359Z_m7b2-complementary-boundary-fusion_seedpanel/m7b2_fusion_summary.csv`
- manifest: `configs/artifacts/m7b2/20260323T093359Z_m7b2-complementary-boundary-fusion_seedpanel/manifest.json`

## Supported Conclusion

Within the current synthetic stream suite, the compression-derived feature family looks more natural as a segmentation / change-point helper than as a replacement detector. RMS-normalized mean compressed byte value can provide occasional complementary boundary information, but simple fusion with ED and CAV did not yield strong aggregate gains across the tested stream families.

## Non-Conclusions

M7 did **not** show any of the following:

- a result on the original paper's private dataset,
- an SDR or over-the-air result,
- proof of deployment usefulness,
- proof that the segmentation/helper role is broadly strong,
- proof that more sophisticated fusion would or would not help,
- removal of the current compression-backend caveat.

## Current Best Applied Interpretation

The current best applied read is narrow:

- the compression-derived cue still does not look like a strong classifier / detector replacement for ED or CAV,
- it looks more plausible as a secondary segmentation / boundary cue,
- it can occasionally recover transition cases that the baseline signals miss,
- but the checked-in simple fusion setting showed only modest practical lift in aggregate.

## Open Question / Next Step

The next natural question is whether this answer is already sufficient for the original research goal, or whether one more narrow application-role check is warranted, such as anomaly flagging / triage or a more task-specific but still simple fusion strategy. This document does not commit the repository to that next step; it only records that the current M7 answer remains modest and role-specific.

## Compact Status Table

| Milestone | Question | Key result | Current status / implication | Artifact path |
| --- | --- | --- | --- | --- |
| M7b | If the compression-derived family is not a replacement detector, does it help as a stream change-point / segmentation cue? | ED stayed strongest on `quiet-to-structured-regime`; RMS-normalized mean stayed secondary overall but was competitive on correlated-nuisance and structure-shift families and logged distinct onset-hit cases the best baseline missed. | Supports a helper / segmentation framing more than a replacement-detector framing. | `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/` |
| M7b2 | Does simple normalized-average boundary fusion make that helper role practically stronger? | Fusion was best or tied-best on 23/54 conditions, but median onset-hit lift over the best single signal was 0.000 in all three families; recovery cases existed but were modest. | Occasional complementary boundary information exists, but strong aggregate gain from simple fusion was not demonstrated. | `configs/artifacts/m7b2/20260323T093359Z_m7b2-complementary-boundary-fusion_seedpanel/` |
