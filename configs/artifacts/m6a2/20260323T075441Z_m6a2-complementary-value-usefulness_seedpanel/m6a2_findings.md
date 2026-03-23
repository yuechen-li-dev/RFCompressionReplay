# M6a2 Complementary-Value Usefulness Findings

## Scope

- Task families: `engineered-structure-vs-natural-correlation`, `equal-energy-engineered-structure-vs-natural-correlation`
- Detector panel: `ed`, `cav`, `lzmsa-paper`, `lzmsa-rms-normalized-mean-compressed-byte-value`
- Bundle comparison: `bundle-a-ed-cav` = [ed, cav], `bundle-b-ed-cav-rms-normalized-mean` = [ed, cav, lzmsa-rms-normalized-mean-compressed-byte-value]
- Bundle readout: deterministic leave-one-seed-out logistic regression trained separately within each `(task family, SNR, window length)` condition and evaluated on the held-out seed only.
- SNR values (dB): -9, -3, 0
- Window lengths: 64, 128
- Seeds: 86420, 97531, 24680
- Trial count per class per seed/condition: 48
- Retention mode used: `milestone` (top-level M6a2 retention keeps only manifest + compact standalone comparison + compact bundle summary + findings markdown)
- Config provenance: `m6a2-complementary-value-usefulness` / `m6a2`

## Standalone Detector Read

### `engineered-structure-vs-natural-correlation`

- Best baseline by median AUC: `cav` at 0.275.
- Best compression-derived standalone detector: `lzmsa-rms-normalized-mean-compressed-byte-value` at 0.495.
- RMS-normalized mean vs `lzmsa-paper`: 0.495 vs 0.437 median AUC.
- Cautious read: compression-derived standalone features became genuinely competitive here and sometimes surpassed the strongest baseline within the tested grid.

### `equal-energy-engineered-structure-vs-natural-correlation`

- Best baseline by median AUC: `ed` at 0.529.
- Best compression-derived standalone detector: `lzmsa-rms-normalized-mean-compressed-byte-value` at 0.551.
- RMS-normalized mean vs `lzmsa-paper`: 0.551 vs 0.539 median AUC.
- Cautious read: compression-derived standalone features became closer to ED/CAV than in M6a1, but ED/CAV still stayed slightly better on median AUC.

## Bundle Read

### `engineered-structure-vs-natural-correlation`

- Bundle A `[ED, CAV]`: median AUC 0.551, max AUC 0.791.
- Bundle B `[ED, CAV, RMS-normalized mean compressed byte value]`: median AUC 0.562, max AUC 0.736.
- Bundle B minus Bundle A: median 0.005, max 0.244, with 10/18 held-out seed conditions at or above Bundle A by at least 0.001 AUC.
- Cautious read: adding RMS-normalized mean helped modestly and fairly consistently, which fits a complementary-feature interpretation better than a replacement-detector story.

### `equal-energy-engineered-structure-vs-natural-correlation`

- Bundle A `[ED, CAV]`: median AUC 0.603, max AUC 0.833.
- Bundle B `[ED, CAV, RMS-normalized mean compressed byte value]`: median AUC 0.565, max AUC 0.786.
- Bundle B minus Bundle A: median -0.005, max 0.122, with 8/18 held-out seed conditions at or above Bundle A by at least 0.001 AUC.
- Cautious read: the added feature occasionally helped, but the gain was condition-local rather than broad across the grid.

## Overall Conclusion

- Within this synthetic suite, compression-derived standalone detectors became somewhat more competitive on the fairer task families, but their more practical role still looks complementary: RMS-normalized mean added modest value to the tiny ED+CAV bundle more clearly than it replaced ED or CAV outright.

## Caveats

- This remains a synthetic-only benchmark suite.
- The engineered structured processes are OFDM-like / repeated-frame-like synthetic constructions, not LTE-faithful signals.
- The current deterministic compression-backend caveat remains in force.
- No SDR-facing or deployment-readiness claim is made here.
- M6a2 is still usefulness mapping inside the current harness, not deployment proof.
