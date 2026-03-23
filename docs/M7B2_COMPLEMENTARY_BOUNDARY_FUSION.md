# M7b2 Complementary Boundary Fusion

M7b2 is the direct follow-up to M7b's stream-level change-point usefulness mapping.

Question under test:

> If RMS-normalized mean compressed byte value sometimes catches transition onsets that the best baseline misses, does a simple fused boundary score outperform the individual detector traces on the current M7b stream task families?

## Scope

M7b2 keeps the same disciplined boundaries as M7b:

- synthetic streams only,
- exactly the same three transition families,
- the same standalone signal panel (`ed`, `cav`, and `lzmsa-rms-normalized-mean-compressed-byte-value`),
- one simple explicit fusion rule,
- the same small sliding-window / adjacent-change / peak-picking boundary pipeline,
- compact summary-first artifacts by default.

This is **not** an SDR milestone, **not** an LTE-fidelity milestone, and **not** a learned ensemble system.

## Stream Task Families

M7b2 reuses the M7b stream suite unchanged:

1. `quiet-to-structured-regime`
2. `correlated-nuisance-to-engineered-structure`
3. `structure-to-structure-regime-shift`

## Fusion Rule

The checked-in M7b2 run tests one deterministic fused boundary signal:

- `ed-cav-rms-normalized-mean-fused`

Rule:

1. compute each standalone detector's sliding-window score trace,
2. convert each to an absolute adjacent-window change trace,
3. min-max normalize each change trace to `[0, 1]` **within stream**,
4. average the normalized traces pointwise,
5. apply the same M7b peak-threshold / spacing / maximum-proposal rule to that fused trace.

This keeps the comparison auditable and aligned with M7b rather than introducing a new heavy post-processing stack.

## Compact Artifacts

The checked-in M7b2 run keeps only:

- `manifest.json`
- `m7b2_boundary_comparison.csv`
- `m7b2_fusion_summary.csv`
- `m7b2_findings.md`

Milestone retention is used as the repository's nearest compact summary-first retention mode for this phase.
