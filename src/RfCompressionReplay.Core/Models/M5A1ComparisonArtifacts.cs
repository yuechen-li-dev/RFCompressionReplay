namespace RfCompressionReplay.Core.Models;

public sealed record M5A1ComparisonRow(
    string TaskName,
    double ConditionSnrDb,
    int WindowLength,
    double PaperAuc,
    double CompressedLengthAuc,
    double NormalizedCompressedLengthAuc,
    double MeanCompressedByteValueAuc,
    double PaperMinusCompressedLength,
    double PaperMinusNormalizedCompressedLength,
    double PaperMinusMeanCompressedByteValue);

public sealed record M5A1AggregateDeltaRow(
    string AlternativeDetectorName,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper);

public sealed record M5A1ComparisonReport(
    IReadOnlyList<M5A1ComparisonRow> Rows,
    IReadOnlyList<M5A1AggregateDeltaRow> AggregateDeltaRows,
    string FindingsMarkdown);
