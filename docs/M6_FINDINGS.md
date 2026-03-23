# M6 Findings Freeze

M6 freezes the repository's usefulness-mapping arc for the refined compression-derived detector family.

## M6 Question

After the M5 mechanism work, M6 asks two practical synthetic questions:

1. Can the refined compression-derived detector family do something usefully discriminative on small synthetic sensing-style tasks?
2. Is the family better understood as a replacement detector for ED/CAV, or as a complementary feature that may add value alongside them?

## Scope

M6 covered:

- a synthetic-only benchmark, not captured-data or SDR validation,
- M6a1's small applied task suite,
- M6a2's fairer engineered-structure-vs-natural-correlation task suite,
- standalone detector comparison plus a tiny explicit bundle comparison in M6a2,
- compact retained artifacts intended to keep milestone conclusions easy to audit,
- usefulness mapping only, not deployment validation.

## Milestone-by-Milestone Summary

### M6a1

M6a1 was the first usefulness-mapping milestone. It compared `ed`, `cav`, `lzmsa-paper`, and `lzmsa-rms-normalized-mean-compressed-byte-value` on three compact synthetic task families. In the checked-in artifacts, ED dominated `colored-nuisance-vs-white-noise`, CAV dominated `structured-burst-vs-noise-only` and `equal-energy-structured-vs-unstructured`, compression-derived detectors did not lead a task family overall, and RMS-normalized mean was generally the better practical compression-derived proxy even though it still trailed CAV substantially. Practical read: in this suite, the compression-derived family looked secondary rather than like a strong replacement for ED/CAV.

Relevant compact artifacts:

- `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/m6a1_findings.md`
- `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/m6a1_auc_comparison.csv`
- `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/m6a1_task_summary.csv`
- `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/manifest.json`

### M6a2

M6a2 was the fairer complementary-value follow-on milestone. It kept the focused standalone detector panel, shifted the task families toward engineered structure versus natural correlation, and added a tiny bundle comparison between Bundle A = `[ED, CAV]` and Bundle B = `[ED, CAV, RMS-normalized mean compressed byte value]`. In the checked-in artifacts, the compression-derived standalone detectors became more competitive than in M6a1, RMS-normalized mean beat `lzmsa-paper` on both M6a2 task families, and Bundle B showed only a modest median gain on `engineered-structure-vs-natural-correlation` while being slightly negative on the equal-energy family. Practical read: the checked-in result fits a complementary-value framing more than a replacement-detector framing.

Relevant compact artifacts:

- `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/m6a2_findings.md`
- `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/m6a2_auc_comparison.csv`
- `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/m6a2_bundle_summary.csv`
- `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/manifest.json`

## Supported Conclusion

Within the current synthetic suites, compression-derived detectors did not emerge as strong replacements for ED/CAV. RMS-normalized mean compressed byte value is the better practical descendant of the original `lzmsa-paper` statistic among the simple checked-in proxies. On the fairer complementary-value suite, compression-derived features became more competitive, but they added only modest practical value on top of `ED + CAV`.

## Non-Conclusion

M6 did **not** show:

- any claim about the original paper's private dataset,
- an SDR or captured-RF result,
- proof of deployment usefulness,
- proof that the compression-derived family is useless in all roles,
- proof that no other task family or application role would benefit,
- removal of the current compression-backend caveat.

## Current Best Practical Interpretation

The current best repository-backed interpretation is:

- the phenomenon is mechanistically real, because the M5 arc established repeatable compression-derived structure sensitivity,
- RMS-normalized mean compressed byte value is the strongest practical simple proxy identified so far,
- the family currently looks more complementary than replacement-oriented,
- practical gains over ED/CAV are limited in the tested compact synthetic suites.

## Open Question / Next Step

The next natural question is not whether M6 already proved detector replacement, but whether the same feature family helps more in other application roles such as anomaly flagging, segmentation, triage, or regime-change / change-point settings where complementary structure sensitivity may matter more than outright standalone detector leadership.

## Compact Status Table

| Milestone | Question | Key result | Current status / implication | Artifact path |
| --- | --- | --- | --- | --- |
| M6a1 | Do compression-derived detectors look practically useful against ED/CAV on a small synthetic sensing-style suite? | ED led `colored-nuisance-vs-white-noise`; CAV led the two structure-focused families; compression-derived detectors did not lead overall; RMS-normalized mean was usually the better compression-derived proxy. | Replacement story not supported on the first suite; compression-derived features looked secondary. | `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/` |
| M6a2 | Do compression-derived features look better on fairer engineered-structure-vs-natural-correlation tasks, and does RMS-normalized mean add value inside `[ED, CAV]`? | Standalone compression-derived detectors became more competitive; RMS-normalized mean beat `lzmsa-paper` on both families; adding it to `[ED, CAV]` produced only a modest median gain on one family and a slight median loss on the equal-energy family. | Best current framing is complementary value with limited practical gains, not detector replacement. | `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/` |
