# M7b Change-Point / Segmentation Usefulness Findings

## Scope

- Stream task families: `quiet-to-structured-regime`, `correlated-nuisance-to-engineered-structure`, `structure-to-structure-regime-shift`
- Detector panel: `ed`, `cav`, `lzmsa-rms-normalized-mean-compressed-byte-value`
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds: 86420, 97531, 24680
- Stream count per seed/condition: 8
- Retention mode used: `milestone` (compact summary-first: manifest + boundary comparison CSV + task summary CSV + findings markdown)
- Config provenance: `m7b-change-point-usefulness` / `m7b`

## Task-Family Read

### `quiet-to-structured-regime`

- Best overall boundary cue by median onset hit rate: `ed` with onset hit rate 0.375, median onset error 32.000, and median false positives 2.000.
- Best baseline: `ed` with onset hit rate 0.375.
- RMS-normalized mean compressed byte value: onset hit rate 0.125, onset error 64.000, median false positives 3.000, distinct-hit conditions vs best baseline 0.
- Cautious read: ED/CAV remained clearly stronger here, and RMS-normalized mean looked secondary rather than replacement-grade.

### `correlated-nuisance-to-engineered-structure`

- Best overall boundary cue by median onset hit rate: `cav` with onset hit rate 0.125, median onset error 64.000, and median false positives 3.000.
- Best baseline: `cav` with onset hit rate 0.125.
- RMS-normalized mean compressed byte value: onset hit rate 0.125, onset error 64.000, median false positives 3.000, distinct-hit conditions vs best baseline 2.
- Cautious read: RMS-normalized mean was competitive on hit rate and showed some distinct complementary boundary behavior, even though it was not a universal winner.

### `structure-to-structure-regime-shift`

- Best overall boundary cue by median onset hit rate: `ed` with onset hit rate 0.125, median onset error 48.000, and median false positives 3.000.
- Best baseline: `ed` with onset hit rate 0.125.
- RMS-normalized mean compressed byte value: onset hit rate 0.125, onset error 64.000, median false positives 3.000, distinct-hit conditions vs best baseline 2.
- Cautious read: RMS-normalized mean was competitive on hit rate and showed some distinct complementary boundary behavior, even though it was not a universal winner.

## Overall Role Read

- RMS-normalized mean was outright best on 13 of 54 evaluated seed-conditions.
- Across task-family summaries, its median onset hit rate ranged from 0.125 to 0.125, versus baseline family leaders ranging from 0.125 to 0.375.
- Distinct-hit evidence: RMS-normalized mean recorded 4 condition-level cases where it hit an onset while the best baseline missed under the same `(task, seed, SNR, windowLength)` condition.
- Cautious practical read: the proxy remained secondary overall, but it showed some distinct boundary behavior that could matter as a lightweight segmentation helper rather than a replacement detector.

## Overall Conclusion

- Within this synthetic stream suite, RMS-normalized mean compressed byte value remained secondary overall, but it produced distinct boundary behavior in a small number of conditions and therefore looks more plausible as a segmentation helper than as a replacement classifier.

## Caveats

- This remains a synthetic-only stream benchmark suite.
- The engineered regime constructions are simple OFDM-like / correlated-process transitions, not LTE-fidelity modeling.
- The current deterministic compression-backend caveat remains in force.
- No SDR-facing or deployment-readiness claim is made here.
- M7b is stream-level change-point usefulness mapping, not deployment proof or a large segmentation framework.
