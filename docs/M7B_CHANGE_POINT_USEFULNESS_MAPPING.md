# M7b Change-Point / Segmentation Usefulness Mapping

M7b is the first stream-level usefulness check for the refined compression-derived feature family after the M6 static-window usefulness pass.

Question under test:

> On synthetic streams with known regime transitions, does RMS-normalized mean compressed byte value provide useful boundary / change-point information, either alone or as a complementary cue relative to ED and CAV?

## Scope

M7b is intentionally small and explicit:

- synthetic streams only,
- exactly three stream task families,
- a compact detector panel,
- simple sliding-window score traces,
- a simple deterministic change score,
- compact summary-first artifacts by default.

This is **not** an SDR milestone, **not** an LTE-fidelity milestone, and **not** a large segmentation framework.

## Stream Task Families

### 1. `quiet-to-structured-regime`

A long stream with:

- noise-only prefix,
- sustained OFDM-like structured middle regime,
- noise-only suffix.

This family has known onset and offset boundaries and asks whether a detector helps localize the structured segment.

### 2. `correlated-nuisance-to-engineered-structure`

A long stream with:

- correlated Gaussian nuisance prefix,
- engineered OFDM-like structure in the middle,
- correlated Gaussian nuisance suffix.

Both sides are non-iid and both use the same configured SNR panel so the transition is not meant to collapse to a raw energy-only jump.

### 3. `structure-to-structure-regime-shift`

A long stream with:

- one OFDM-like structured regime,
- followed by a second OFDM-like regime with different subcarrier organization / symbol timing.

This family checks whether the signal helps with structure-to-structure change detection, not only quiet-vs-signal separation.

## Detector Panel

M7b keeps the panel intentionally compact:

- `ed`
- `cav`
- `lzmsa-rms-normalized-mean-compressed-byte-value`

`lzmsa-paper` is intentionally omitted from the default M7b checked-in run to keep the panel focused on the practical simple proxy identified by M6.

## Boundary Proposal Procedure

For each `(task family, seed, SNR, window length, detector)` condition:

1. compute detector scores on overlapping sliding windows,
2. form a change trace using the absolute adjacent-window score difference,
3. detect local peaks above a robust median-plus-MAD threshold,
4. keep at most three candidate boundaries under a minimum-spacing rule,
5. evaluate proposals against the known boundary locations.

The same deterministic procedure is used for every detector.

## Compact Artifacts

The checked-in M7b run keeps only:

- `manifest.json`
- `m7b_boundary_comparison.csv`
- `m7b_task_summary.csv`
- `m7b_findings.md`

No large per-window trace dump is retained by default for the milestone run.
