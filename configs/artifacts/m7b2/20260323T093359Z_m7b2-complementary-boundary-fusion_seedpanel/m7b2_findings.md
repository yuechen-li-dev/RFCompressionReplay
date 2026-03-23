# M7b2 Complementary Boundary Fusion Findings

## Scope

- Stream task families: `quiet-to-structured-regime`, `correlated-nuisance-to-engineered-structure`, `structure-to-structure-regime-shift`
- Standalone signals: `ed`, `cav`, `lzmsa-rms-normalized-mean-compressed-byte-value`
- Fusion signal: `ed-cav-rms-normalized-mean-fused`
- Fusion rule: `normalized-adjacent-change-minmax-average` = compute each detector's absolute adjacent-window change trace, normalize each trace to [0, 1] within stream via min-max scaling, then average the normalized traces pointwise before the same peak-picking boundary rule.
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds: 86420, 97531, 24680
- Stream count per seed/condition: 8
- Retention mode used: `milestone` (repository milestone mode used as the nearest compact summary-first retention path: manifest + boundary comparison CSV + fusion summary CSV + findings markdown)
- Config provenance: `m7b2-complementary-boundary-fusion` / `m7b2`

## Task-Family Read

### `correlated-nuisance-to-engineered-structure`

- Best single signal: `cav` with median onset hit rate 0.125, median onset error 64.000, and median false positives 3.000.
- Fused signal `ed-cav-rms-normalized-mean-fused`: median onset hit rate 0.125, median onset error 0.000, median false positives 3.000, fused-minus-best-single onset hit delta 0.000, false-positive delta 0.000.
- Fusion best/tied-best conditions: 7; recovery vs best baseline: 0; recovery vs all singles: 0.
- Cautious read: Fusion was roughly competitive with the best single signal on this family, but the gain looked marginal rather than clearly practical.

### `quiet-to-structured-regime`

- Best single signal: `ed` with median onset hit rate 0.375, median onset error 32.000, and median false positives 2.000.
- Fused signal `ed-cav-rms-normalized-mean-fused`: median onset hit rate 0.375, median onset error 32.000, median false positives 2.000, fused-minus-best-single onset hit delta 0.000, false-positive delta 0.000.
- Fusion best/tied-best conditions: 9; recovery vs best baseline: 1; recovery vs all singles: 1.
- Cautious read: Fusion showed complementary value on this family by recovering some onset-hit cases that the singles missed, but the overall lift remained modest; RMS-normalized mean looks helpful as a secondary boundary cue rather than a dominant standalone detector. (normalized-mean median onset hit rate 0.125)

### `structure-to-structure-regime-shift`

- Best single signal: `ed` with median onset hit rate 0.125, median onset error 48.000, and median false positives 3.000.
- Fused signal `ed-cav-rms-normalized-mean-fused`: median onset hit rate 0.125, median onset error 64.000, median false positives 3.000, fused-minus-best-single onset hit delta 0.000, false-positive delta 0.000.
- Fusion best/tied-best conditions: 7; recovery vs best baseline: 1; recovery vs all singles: 0.
- Cautious read: Fusion showed complementary value on this family by recovering some onset-hit cases that the singles missed, but the overall lift remained modest; RMS-normalized mean looks helpful as a secondary boundary cue rather than a dominant standalone detector. (normalized-mean median onset hit rate 0.125)

## Overall Role Read

- Fusion was best or tied-best on 23 of 54 evaluated `(task, seed, SNR, windowLength)` conditions.
- Fusion recovered onset-hit conditions missed by the best baseline in 2 family-condition summaries, and by all three standalone signals in 1 family-condition summaries.
- Cautious practical read: the compression-derived cue looks only weakly complementary in simple fusion: it sometimes helps recover missed boundaries, but the aggregate lift stays modest.

## Overall Conclusion

- Fusion produced only marginal gains overall, so the compression-derived cue remains secondary but occasionally helpful, especially via condition-level recoveries on `quiet-to-structured-regime`, `structure-to-structure-regime-shift`.

## Caveats

- This remains a synthetic-only stream benchmark suite.
- The engineered regime constructions are simple OFDM-like / correlated-process transitions, not LTE-fidelity modeling.
- The current deterministic compression-backend caveat remains in force.
- No SDR-facing or deployment-readiness claim is made here.
- M7b2 is compact stream-level usefulness mapping for complementary boundary fusion, not deployment proof.
