namespace RfCompressionReplay.Core.Models;

public sealed record M5A2ComparisonRow(
    string TaskName,
    double ConditionSnrDb,
    int WindowLength,
    double PaperAuc,
    double MeanCompressedByteValueAuc,
    double CompressedByteVarianceAuc,
    double Bucket0To63ProportionAuc,
    double Bucket64To127ProportionAuc,
    double Bucket128To191ProportionAuc,
    double Bucket192To255ProportionAuc,
    double PrefixThirdMeanCompressedByteValueAuc,
    double SuffixThirdMeanCompressedByteValueAuc,
    double PaperMinusMeanCompressedByteValue,
    double PaperMinusCompressedByteVariance,
    double PaperMinusBucket0To63Proportion,
    double PaperMinusBucket64To127Proportion,
    double PaperMinusBucket128To191Proportion,
    double PaperMinusBucket192To255Proportion,
    double PaperMinusPrefixThirdMeanCompressedByteValue,
    double PaperMinusSuffixThirdMeanCompressedByteValue);

public sealed record M5A2AggregateDeltaRow(
    string AlternativeDetectorName,
    string FeatureFamily,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    int CloserThanWholeStreamMeanConditionCount);

public sealed record M5A2ComparisonReport(
    IReadOnlyList<M5A2ComparisonRow> Rows,
    IReadOnlyList<M5A2AggregateDeltaRow> AggregateDeltaRows,
    string FindingsMarkdown);
