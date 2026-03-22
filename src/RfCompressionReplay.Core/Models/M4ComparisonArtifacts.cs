namespace RfCompressionReplay.Core.Models;

public sealed record M4ComparisonRow(
    string TaskName,
    double ConditionSnrDb,
    int WindowLength,
    double PaperAuc,
    double CompressedLengthAuc,
    double NormalizedCompressedLengthAuc,
    double PaperMinusCompressedLength,
    double PaperMinusNormalizedCompressedLength,
    double CompressedLengthMinusNormalizedCompressedLength);

public sealed record M4ComparisonReport(
    IReadOnlyList<M4ComparisonRow> Rows,
    string FindingsMarkdown);
