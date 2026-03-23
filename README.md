# RfCompressionReplay

`RfCompressionReplay` is a .NET 8 experiment harness for an independent reproduction of a 2018 RF spectrum-sensing paper. M5a1 extended the typed M0/M1/M2/M3/M4/M4a harness with the first compressed-stream decomposition pass, M5a2r re-landed the second compressed-stream decomposition pass under the current milestone-retention policy, M5a3 added a narrow multi-seed stability confirmation pass over that same M5a2 feature family, M5b1 added the first representation-perturbation exploration pass, M5b2 refined that pass by separating scale-only versus packing/precision-only perturbations, M5b3 refined the scale side further with an explicit scale panel plus one simple normalization comparison, M5f freezes that arc into a concise repository-native findings package, M6a1 adds the first compact usefulness-mapping pass over a slightly more application-oriented synthetic task suite, M6a2 adds the second usefulness-mapping pass focused on complementary value rather than detector replacement, and M7b adds a compact stream-level change-point / segmentation usefulness pass over three deterministic synthetic transition families.

Current status: within the current synthetic usefulness-mapping suites, compression-derived features did not become strong replacement detectors for ED/CAV. The checked-in M5 arc still supports per-window RMS-normalized mean compressed byte value as the strongest practical simple proxy in this detector family, and the M6 freeze now points to `docs/M6_FINDINGS.md` plus the compact M6a1/M6a2 artifacts under `configs/artifacts/m6a1/` and `configs/artifacts/m6a2/`. M7b extends that question to stream-level regime transitions rather than static windows: in the checked-in synthetic stream suite the RMS-normalized mean feature remained secondary overall, but it produced a small amount of distinct boundary behavior on the correlated-nuisance and structure-shift families, which is a better fit for a cautious segmentation-helper framing than for detector replacement. This remains synthetic-only lab work, not a deployment claim.


## What M7b Adds

- A compact multi-seed **stream-level** change-point / segmentation usefulness runner for exactly three synthetic transition families:
  - `quiet-to-structured-regime`
  - `correlated-nuisance-to-engineered-structure`
  - `structure-to-structure-regime-shift`
- A focused detector panel for this milestone:
  - `ed`
  - `cav`
  - `lzmsa-rms-normalized-mean-compressed-byte-value`
- A tiny explicit and auditable boundary procedure shared across detectors:
  - sliding-window score traces
  - absolute adjacent-window score differences
  - local-peak proposals above a robust median-plus-MAD threshold
  - minimum-spacing filtering
- Compact summary-first M7b artifacts:
  - `manifest.json`
  - `m7b_boundary_comparison.csv`
  - `m7b_task_summary.csv`
  - `m7b_findings.md`
- Default top-level retention for M7b keeps only those compact outputs for the checked-in run rather than large per-window trace dumps.

## What M6a1 Adds

- A compact multi-seed usefulness-mapping runner for a focused three-task synthetic suite:
  - `structured-burst-vs-noise-only`
  - `colored-nuisance-vs-white-noise`
  - `equal-energy-structured-vs-unstructured`
- A focused detector panel:
  - `ed`
  - `cav`
  - `lzmsa-paper`
  - `lzmsa-rms-normalized-mean-compressed-byte-value`
- Two new simple synthetic source constructions used by that suite:
  - a weak localized OFDM-like burst embedded in noise,
  - a modest correlated Gaussian nuisance process.
- Compact summary-first M6a1 artifacts:
  - `manifest.json`
  - `m6a1_auc_comparison.csv`
  - `m6a1_task_summary.csv`
  - `m6a1_findings.md`
- Default top-level retention for M6a1 keeps only those compact outputs for the checked-in run rather than per-trial dumps or raw ROC tables.

## What M6a2 Adds

- A second compact multi-seed usefulness-mapping runner focused on **complementary value** rather than replacement:
  - `engineered-structure-vs-natural-correlation`
  - `equal-energy-engineered-structure-vs-natural-correlation`
- The same focused standalone detector panel as M6a1:
  - `ed`
  - `cav`
  - `lzmsa-paper`
  - `lzmsa-rms-normalized-mean-compressed-byte-value`
- A tiny explicit bundle comparison:
  - Bundle A = `[ED, CAV]`
  - Bundle B = `[ED, CAV, RMS-normalized mean compressed byte value]`
- A deterministic leave-one-seed-out logistic readout for the bundle comparison, trained separately within each `(task family, SNR, window length)` condition so the procedure stays auditable and leakage-light.
- Compact summary-first M6a2 artifacts:
  - `manifest.json`
  - `m6a2_auc_comparison.csv`
  - `m6a2_bundle_summary.csv`
  - `m6a2_findings.md`
- Default top-level retention for M6a2 keeps only those compact outputs for the checked-in run rather than per-trial dumps or raw ROC tables.

## What M5a1 Adds

- Config-driven named synthetic evaluation tasks for:
  - `ofdm-signal-present-vs-noise-only`
  - `gaussian-emitter-vs-noise-only`
- Config-driven sweeps over:
  - detector list
  - SNR dB values
  - window lengths
  - per-condition Monte Carlo trial counts
- Per-trial binary-label score collection suitable for ROC/AUC.
- Deterministic ROC point generation and trapezoidal AUC calculation per `(task, detector, snrDb, windowLength)` condition.
- Machine-readable evaluation artifacts with explicit retention modes:
  - `summary.json`
  - `summary.csv`
  - `trials.csv` in `full` mode
  - `roc_points.csv` in `full` mode
  - `roc_points_compact.csv` in `milestone` mode
- M4 or M4a comparison artifacts when the evaluation run compares the three compression-derived detector identities together:
  - `m4_auc_comparison.csv`
  - `m4_findings.md`
  - `m4a_auc_comparison.csv`
  - `m4a_findings.md`
- M5a1 decomposition artifacts when the evaluation run compares the four compression-derived detector identities together:
  - `m5a1_auc_comparison.csv`
  - `m5a1_findings.md`
  - `m5a1_delta_summary.csv`
- M5a2 decomposition artifacts when the evaluation run compares the intended coarse compressed-byte summary family together:
  - `m5a2_auc_comparison.csv`
  - `m5a2_findings.md`
  - `m5a2_delta_summary.csv`
- M5a3 stability artifacts when the current M5a2 feature family is rerun across an explicit multi-seed panel:
  - `m5a3_auc_comparison.csv`
  - `m5a3_delta_summary.csv`
  - `m5a3_stability_summary.csv`
  - `m5a3_findings.md`
- M5b1 representation-perturbation artifacts when the focused M5a-neighborhood panel is rerun across an explicit perturbation × seed matrix:
  - `m5b1_auc_comparison.csv`
  - `m5b1_delta_summary.csv`
  - `m5b1_perturbation_stability_summary.csv`
  - `m5b1_findings.md`
- M5b2 perturbation-axis refinement artifacts when the same focused panel is rerun across explicit baseline / scale-only / packing-only / combined representation conditions:
  - `m5b2_auc_comparison.csv`
  - `m5b2_delta_summary.csv`
  - `m5b2_axis_summary.csv`
  - `m5b2_findings.md`
- M5b3 scale-handling refinement artifacts when the same focused panel is rerun across explicit raw-scaled / normalized representation families and a compact multi-scale panel:
  - `m5b3_auc_comparison.csv`
  - `m5b3_delta_summary.csv`
  - `m5b3_scale_summary.csv`
  - `m5b3_findings.md`
- Manifest metadata that records task names and sweep axes.
- Focused xUnit coverage for ROC/AUC sanity, score orientation, condition grouping, end-to-end sample configs, and determinism.

## Scientific Intent

M4 and M4a are about **score-identity mechanism comparison**, not paper-number reproduction.

The harness now supports synthetic comparisons that mirror the paper's experiment shape closely enough to inspect detector behavior honestly:

1. **Signal-present vs noise-only** using an OFDM-like synthetic source mixed into Gaussian background noise for the positive class.
2. **Gaussian-emitter control vs noise-only baseline** using an independent Gaussian signal process mixed into Gaussian background noise for the positive class.

These are synthetic benchmark tasks with explicit labels, not magical label inference.

Important caveats:

- Results remain independently regenerated by this harness.
- The OFDM-like source is **not LTE** and is not claimed to be standards-faithful.
- The Gaussian-emitter control is intentionally simple and Gaussian-like.
- `lzmsa-paper` still uses the currently implemented deterministic compression backend and the paper-style **byte-sum-over-compressed-bytes** score contract.
- M4 and M4a do **not** change the compression backend, synthetic task definitions, or ROC/AUC layer; they only compare score identity while keeping that path fixed.
- M3 does **not** claim exact replication of the paper's original unpublished numbers.

## Compression-Derived Detector Variants

The repository now exposes four separate compression-derived detector IDs that all share the same scalar-window serialization and Brotli compression path:

- `lzmsa-paper` (`paper-byte-sum`)
  - Formula: `score = sum(compressedBytes)`
  - Orientation: `HigherScoreMorePositive`
  - Purpose: preserve the existing paper-style byte-sum score unchanged.
- `lzmsa-compressed-length` (`compressed-byte-count`)
  - Formula: `score = compressedByteCount`
  - Orientation: `LowerScoreMorePositive`
  - Purpose: compare the paper-style statistic against raw compressed length while keeping the same payload basis.
- `lzmsa-normalized-compressed-length` (`compressed-byte-count-per-input-byte`)
  - Formula: `score = compressedByteCount / inputByteCount`
  - Orientation: `LowerScoreMorePositive`
  - Purpose: compare against a normalized compression-length metric on the same serialized input bytes.
- `lzmsa-mean-compressed-byte-value` (`mean-compressed-byte-value`)
  - Formula: `score = compressedByteSum / compressedByteCount`
  - Orientation: `HigherScoreMorePositive`
  - Purpose: keep the compression path fixed while isolating the mean-byte-value factor from the identity `byteSum = compressedLength × meanCompressedByteValue`.
- M5a2r adds:
  - `lzmsa-compressed-byte-variance`
  - `lzmsa-compressed-byte-bucket-0-63-proportion`
  - `lzmsa-compressed-byte-bucket-64-127-proportion`
  - `lzmsa-compressed-byte-bucket-128-191-proportion`
  - `lzmsa-compressed-byte-bucket-192-255-proportion`
  - `lzmsa-prefix-third-mean-compressed-byte-value`
  - `lzmsa-suffix-third-mean-compressed-byte-value`
  - Purpose: test whether coarse distribution or positional compressed-byte summaries explain `lzmsa-paper` better than whole-stream mean compressed byte value alone.

`inputByteCount` is the serialized scalar-window payload size in bytes before compression. The serialization contract itself is unchanged.

M5b1/M5b2/M5b3 introduce a **configurable representation contract** for the compression-derived detector family only:

- `sampleScale`: deterministic multiplicative factor applied to each scalar before serialization
- `numericFormat`:
  - `float64-le` for the original baseline
  - `float32-le` for the first packing/precision perturbation
- `normalizationMode`:
  - `none` for the raw serialization path
  - `rms` for per-window RMS normalization before serialization
- `normalizationTarget`: fixed positive target used by the selected normalization rule

M5b1 introduced scale and packing perturbations without extra normalization, M5b2 split those perturbations into explicit axes, and M5b3 adds exactly one simple normalization comparison: normalize each window to target RMS 1.0 before float64-le serialization. No compression-backend change is introduced by this pass.

## Synthetic Tasks Evaluated in M3

### 1. OFDM-like signal present vs noise only

- **Positive class:** OFDM-like structured signal + Gaussian background noise at the configured SNR.
- **Negative class:** Gaussian noise only.
- **Interpretation:** this is the main structured-signal-present versus baseline task.

### 2. Gaussian-emitter control vs noise only

- **Positive class:** independent Gaussian emitter + Gaussian background noise at the configured SNR.
- **Negative class:** Gaussian noise only.
- **Interpretation:** this is an explicit control task used to compare a signal-present case that remains Gaussian-like against the same background-noise baseline.

## Score Orientation and ROC/AUC

M3 makes detector score direction explicit.

Current orientation contract:

- `ed`: higher score means more evidence for the positive class.
- `cav`: higher score means more evidence for the positive class.
- `lzmsa-paper`: higher score means more evidence for the positive class.
- `lzmsa-compressed-length`: lower score means more evidence for the positive class.
- `lzmsa-normalized-compressed-length`: lower score means more evidence for the positive class.
- `lzmsa-mean-compressed-byte-value`: higher score means more evidence for the positive class.

ROC/AUC is computed per condition by:

1. sorting scores according to the detector's documented orientation,
2. walking distinct score thresholds,
3. emitting ROC points `(FPR, TPR)`, and
4. integrating with the trapezoidal rule.

No large ML dependency is used for this step.

Threshold pass/fail also follows the detector's documented orientation. The legacy `IsAboveThreshold` / `AboveThresholdCount` artifact fields therefore mean “meets the configured detector threshold according to that detector's orientation,” even for lower-is-more-positive detector identities.

## Produced Artifacts

Each run writes a deterministic per-run folder beneath the configured output root and records the chosen retention mode in `manifest.json`.

Retention modes:

- `full`: local exploratory/debug output; keeps raw `trials.csv` and raw `roc_points.csv`.
- `milestone`: checked-in result preservation; keeps compact summaries, findings, comparison CSVs, and `roc_points_compact.csv` instead of raw ROC/trial exhaust.
- `smoke`: minimal regression output; keeps manifest, summary artifacts, and milestone findings/comparison files when produced.

Core retained artifacts:

- `manifest.json`: run metadata, config path, environment summary, warnings, M3 sweep/task metadata when evaluation mode is active, and retention-policy metadata describing omitted artifact families plus how to regenerate them.
- `summary.json`: grouped summary objects.
- `summary.csv`: tabular per-condition score summary including class counts and AUC.
- `trials.csv`: per-trial score records including task name, label, detector, detector mode, score orientation, condition SNR, source SNR, and window length in `full` mode only.
- `roc_points.csv`: raw ROC thresholds, TPR/FPR points, class counts, and per-condition AUC in `full` mode only.
- `roc_points_compact.csv`: deterministic downsampled ROC representation retained in `milestone` mode.
- `m4_auc_comparison.csv` / `m4a_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper`, `lzmsa-compressed-length`, and `lzmsa-normalized-compressed-length` by task, SNR, and window length when all three are run together.
- `m4_findings.md` / `m4a_findings.md`: concise scope, comparison table, findings text, and caveats for an M4/M4a score-identity comparison run.
- `m5a1_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper`, `lzmsa-compressed-length`, `lzmsa-normalized-compressed-length`, and `lzmsa-mean-compressed-byte-value` by task, SNR, and window length when all four are run together.
- `m5a1_findings.md`: concise scope, condition summary, decomposition-focused findings text, and caveats for an M5a1 run.
- `m5a1_delta_summary.csv`: compact aggregate median/max absolute AUC deltas between `lzmsa-paper` and each alternative M5a1 identity.
- `m5a2_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper` and the intended M5a2 coarse summary detectors by task, SNR, and window length.
- `m5a2_findings.md`: concise scope, condition summary, key finding, and caveats for an M5a2r run.
- `m5a2_delta_summary.csv`: compact aggregate median/max absolute AUC deltas between `lzmsa-paper` and each alternative M5a2 detector, including feature-family labels and the count of conditions where a detector beat whole-stream mean compressed byte value.
- `m5a3_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper` and the current M5a2 feature set by `seed`, task, SNR, and window length.
- `m5a3_delta_summary.csv`: per-feature median and max absolute AUC deltas from `lzmsa-paper` across all seed-condition combinations in an M5a3 run.
- `m5a3_stability_summary.csv`: per-feature closest-neighbor win count, median/max absolute AUC delta, and median closeness rank across the M5a3 seed panel.
- `m5a3_findings.md`: concise scope, cautious stability interpretation, main conclusion, and caveats for an M5a3 run.
- `m5b1_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper` and the focused M5a-neighborhood panel by `perturbation`, `seed`, task, SNR, and window length.
- `m5b1_delta_summary.csv`: per-perturbation median/max absolute AUC deltas from `lzmsa-paper` for the focused M5b1 feature panel.
- `m5b1_perturbation_stability_summary.csv`: overall and per-perturbation closest-neighbor win count, median/max absolute AUC delta, and median closeness rank for each tested M5b1 feature.
- `m5b1_findings.md`: concise scope, perturbation read, family-level interpretation, and caveats for an M5b1 run.
- `m5b2_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper` and the same focused panel by `perturbationId`, `perturbationAxisTag`, `seed`, task, SNR, and window length.
- `m5b2_delta_summary.csv`: per-perturbation median/max absolute AUC deltas from `lzmsa-paper` for the focused M5b2 feature panel.
- `m5b2_axis_summary.csv`: per-axis closest-neighbor win count, win rate, median/max absolute AUC delta, median closeness rank, and axis-level winners for each tested M5b2 feature.
- `m5b2_findings.md`: concise scope, axis-level read, family-level interpretation, and caveats for an M5b2 run.
- `m5b3_auc_comparison.csv`: side-by-side AUC comparison for `lzmsa-paper` and the same focused panel by `representationFamilyId`, `scaleValue`, `seed`, task, SNR, and window length.
- `m5b3_delta_summary.csv`: per-representation-family closest-neighbor counts plus aggregate median/max absolute AUC deltas from `lzmsa-paper` for the focused M5b3 feature panel.
- `m5b3_scale_summary.csv`: per-family, per-scale closest-neighbor win count, win rate, median/max absolute AUC delta, median closeness rank, and scale-level leaders for each tested M5b3 feature.
- `m5b3_findings.md`: concise scope, scale-sensitivity read, normalization read, family-level interpretation, and caveats for an M5b3 run.

See `docs/ARTIFACT_RETENTION_POLICY.md` for the detailed Mx5 policy, omitted artifact families, and regeneration guidance.

If a same-second rerun would collide, the harness appends a readable suffix such as `_2` to keep artifacts isolated.

## Current Findings and Artifact Pointers

- M4 comparison artifacts: `configs/artifacts/m4/20260322T230355Z_m4-score-identity-comparison_seed12345/`
  - inspect `m4_auc_comparison.csv`, `m4_findings.md`, and `manifest.json`
- M4a confirmation artifacts: `configs/artifacts/m4a/20260322T231911Z_m4a-score-identity-confirmation_seed24680/`
  - inspect `m4a_auc_comparison.csv`, `m4a_findings.md`, and `manifest.json`
- M4b findings freeze: `docs/M4B_FINDINGS.md`
- M5 findings freeze: `docs/M5_FINDINGS.md`
- M6 findings freeze: `docs/M6_FINDINGS.md`
  - compact status of the full M6a1 → M6a2 usefulness-mapping arc, including supported conclusion, non-conclusions, open question, and direct links to the compact milestone artifacts
- M7b method note: `docs/M7B_CHANGE_POINT_USEFULNESS_MAPPING.md`
- M7b change-point artifacts: `configs/artifacts/m7b/20260323T084412Z_m7b-change-point-usefulness_seedpanel/`
  - inspect `m7b_boundary_comparison.csv`, `m7b_task_summary.csv`, `m7b_findings.md`, and `manifest.json`
  - compact stream-level read for the checked-in M7b transition suite, with concise boundary metrics and findings
- M5a1 checked-in artifacts: `configs/artifacts/m5a1/20260322T235141Z_m5a1-compressed-stream-decomposition_seed13579/`
  - inspect `m5a1_auc_comparison.csv`, `m5a1_delta_summary.csv`, `m5a1_findings.md`, and `manifest.json`
- M5a2 checked-in artifacts: `configs/artifacts/m5a2/20260323T014446Z_m5a2r-compressed-stream-decomposition_seed86420/`
  - inspect `m5a2_auc_comparison.csv`, `m5a2_delta_summary.csv`, `m5a2_findings.md`, and `manifest.json`
  - current re-land note: this same-scope rerun on current main materially changed the previously reported unmerged M5a2 finding; the checked-in compact artifacts now show `lzmsa-compressed-byte-bucket-64-127-proportion` as the closest tested simple neighbor to `lzmsa-paper`, with the coarse histogram family narrowly best overall
- M5a3 checked-in artifacts: `configs/artifacts/m5a3/20260323T022136Z_m5a3-stability-confirmation_seedpanel/`
  - inspect `m5a3_stability_summary.csv`, `m5a3_auc_comparison.csv`, `m5a3_findings.md`, and `manifest.json`
  - current stability note: `lzmsa-mean-compressed-byte-value` had the most closest-neighbor wins in the checked-in rerun panel, but not enough to support a stable single-feature winner; the artifact-backed conclusion remains that the nearest-neighbor set stayed split across whole-stream, coarse-positional, and coarse-histogram summaries
- M5b1 checked-in artifacts: `configs/artifacts/m5b1/20260323T051853Z_m5b1-representation-perturbation-exploration_seedpanel/`
  - inspect `m5b1_auc_comparison.csv`, `m5b1_delta_summary.csv`, `m5b1_perturbation_stability_summary.csv`, `m5b1_findings.md`, and `manifest.json`
  - the M5b1 focused panel is intentionally small: whole-stream mean, the current best checked-in histogram representative (`bucket-64-127`), and the current positional representative (`suffix-third mean`)
- M5b2 checked-in artifacts: `configs/artifacts/m5b2/20260323T055613Z_m5b2-perturbation-axis-refinement_seedpanel/`
  - inspect `m5b2_auc_comparison.csv`, `m5b2_delta_summary.csv`, `m5b2_axis_summary.csv`, `m5b2_findings.md`, and `manifest.json`
  - M5b2 keeps the M5b1 focused panel fixed, then separates scale-only from packing-only perturbations so the checked-in artifacts stay summary-first
- M5b3 checked-in artifacts: `configs/artifacts/m5b3/20260323T061933Z_m5b3-scale-handling-refinement_seedpanel/`
  - inspect `m5b3_auc_comparison.csv`, `m5b3_delta_summary.csv`, `m5b3_scale_summary.csv`, `m5b3_findings.md`, and `manifest.json`
  - current scale-handling note: raw scaling reshuffled the median-delta winner at `1.0x`, where `lzmsa-compressed-byte-bucket-64-127-proportion` briefly led on median delta, while the per-window RMS-normalized family kept `lzmsa-mean-compressed-byte-value` as the median-delta leader at every tested scale with fewer winner transitions overall
- M6a1 checked-in artifacts: `configs/artifacts/m6a1/20260323T071814Z_m6a1-usefulness-mapping_seedpanel/`
  - inspect `m6a1_auc_comparison.csv`, `m6a1_task_summary.csv`, `m6a1_findings.md`, and `manifest.json`
  - current usefulness-mapping note: in this compact synthetic suite, ED/CAV remained strongest overall, while the RMS-normalized mean compressed byte value proxy was mainly notable for staying slightly ahead of `lzmsa-paper` on the equal-energy structured-vs-unstructured family rather than for dominating the suite
- M6a2 checked-in artifacts: `configs/artifacts/m6a2/20260323T075441Z_m6a2-complementary-value-usefulness_seedpanel/`
  - inspect `m6a2_auc_comparison.csv`, `m6a2_bundle_summary.csv`, `m6a2_findings.md`, and `manifest.json`
  - current complementary-value note: on the fairer engineered-vs-correlated synthetic tasks, the standalone compression-derived pair became more competitive than in M6a1, but the practical win condition still looks complementary rather than replacement-driven because adding RMS-normalized mean to `[ED, CAV]` only modestly improved the median bundle result on one task family and did not improve the median bundle result on the equal-energy task family
- M5a1 decomposition guide: `docs/M5A1_COMPRESSED_STREAM_DECOMPOSITION.md`
- M5a2 re-land guide: `docs/M5A2R_COMPRESSED_STREAM_DECOMPOSITION.md`
- M5a3 stability guide: `docs/M5A3_STABILITY_CONFIRMATION.md`
- M5b1 perturbation guide: `docs/M5B1_REPRESENTATION_PERTURBATION_EXPLORATION.md`
- M5b2 perturbation guide: `docs/M5B2_PERTURBATION_AXIS_REFINEMENT.md`
- M5b3 scale-handling guide: `docs/M5B3_SCALE_HANDLING_REFINEMENT.md`

## Repository Layout

- `src/RfCompressionReplay.Core/`: typed config, execution flow, detectors, synthetic generators, evaluation logic, and artifact writing.
- `src/RfCompressionReplay.Cli/`: command-line entry point.
- `tests/RfCompressionReplay.Tests/`: xUnit coverage for config validation, detectors, synthetic generation, ROC/AUC, and end-to-end runs.
- `configs/`: runnable M0/M1/M2 sample configs plus M3 evaluation configs.
- `docs/M2_SYNTHETIC_BENCHMARKS.md`: M2 synthetic generator notes.
- `docs/M3_EVALUATION_PROTOCOL.md`: M3 task definitions, sweep semantics, ROC/AUC method, and caveats.
- `docs/M4_SCORE_IDENTITY_COMPARISON.md`: M4 scope, outputs, and reading guide for the score-identity comparison experiment.
- `docs/M4A_CONFIRMATION_RERUN.md`: M4a confirmation-rerun scope, strengthening choices, and reading guide.
- `docs/M4B_FINDINGS.md`: concise M4/M4a findings freeze, supported conclusion, non-conclusions, and next mechanistic question.
- `docs/M5A1_COMPRESSED_STREAM_DECOMPOSITION.md`: M5a1 scope, score decomposition question, outputs, and reading guide.
- `docs/M5A3_STABILITY_CONFIRMATION.md`: M5a3 scope, fixed rerun question, outputs, and reading guide.
- `docs/M6_FINDINGS.md`: concise freeze of the M6a1 → M6a2 usefulness-mapping arc, current supported conclusion, open question, and compact artifact links.
- `docs/M5B1_REPRESENTATION_PERTURBATION_EXPLORATION.md`: M5b1 scope, perturbation panel, focused feature panel, compact outputs, and reading guide.
- `docs/M5B2_PERTURBATION_AXIS_REFINEMENT.md`: M5b2 scope, axis-separated perturbation panel, focused feature panel, compact outputs, and reading guide.
- `docs/M5B3_SCALE_HANDLING_REFINEMENT.md`: M5b3 scope, explicit scale panel, normalization rule, focused feature panel, compact outputs, and reading guide.
- `docs/ARTIFACT_RETENTION_POLICY.md`: Mx5 retention modes, retained-vs-omitted artifact families, compact ROC policy, and regeneration expectations.
- `docs/DETECTOR_IMPLEMENTATION_NOTES.md`: detector formulas and compression-statistic contract.
- `docs/REPRODUCTION_SCOPE.md`: concise reproduction-scope statement.

## Running Sample Experiments

1. Install the .NET 8 SDK.
2. From the repository root, run one of:

```bash
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.noise-only.ed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.gaussian-emitter.ed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.ofdm-like.cav.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m2.mixed.lzmsa-paper.json

dotnet run --project src/RfCompressionReplay.Cli -- configs/m3.ofdm-sweep.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m3.gaussian-control.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m3.lzmsa-compressed-length.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m3.lzmsa-normalized-compressed-length.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m3.mixed.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m4.score-identity-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m4.score-identity-comparison.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m4a.score-identity-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m4a.score-identity-confirmation.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a1.compressed-stream-decomposition-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a1.compressed-stream-decomposition.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a2r.compressed-stream-decomposition-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a2r.compressed-stream-decomposition.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a3.stability-confirmation-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5a3.stability-confirmation.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b1.representation-perturbation-exploration-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b1.representation-perturbation-exploration.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b2.perturbation-axis-refinement-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b2.perturbation-axis-refinement.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b3.scale-handling-refinement-smoke.json
dotnet run --project src/RfCompressionReplay.Cli -- configs/m5b3.scale-handling-refinement.json
```

The checked-in smoke and milestone configs now set `artifactRetentionMode` explicitly; switch a config to `full` for a local rerun when you need omitted raw artifacts such as `trials.csv` or `roc_points.csv`.

On success, the CLI prints the run identifier and the artifact directory.

## Deliberate Scope Simplifications

- Synthetic-only evaluation; no SDR ingestion or external datasets.
- No LTE claim.
- No plotting libraries, notebooks, or large reporting stack.
- No threshold optimization workflow beyond explicit ROC/AUC computation.
- No compression backend swap for this pre-M4 hardening pass.
- No claim that the M4/M4a/M5a1 synthetic comparison settles the paper's original unpublished results.
- M4/M4a findings stay within the synthetic benchmark scope and do not claim anything about private paper data or SDR performance.

## What Later Milestones Can Add

- Further mechanism testing beyond the local M5a1 byte-sum decomposition.
- Broader signal families or external data.
- More standards-faithful waveform generation if later justified.
- Figure-reproduction workflows once the synthetic evaluation layer is stable.
