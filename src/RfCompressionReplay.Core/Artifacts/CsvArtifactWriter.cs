using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Artifacts;

public sealed class CsvArtifactWriter
{
    public void WriteTrials(string path, IReadOnlyList<TrialRecord> trials)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("trialIndex,taskName,scenarioName,targetLabel,classLabel,isPositiveClass,sourceType,detectorName,detectorMode,scoreOrientation,conditionSnrDb,sourceSnrDb,windowLength,windowCount,sampleCount,startIndex,score,isAboveThreshold,meanSample,peakSample");

        foreach (var trial in trials)
        {
            writer.WriteLine(string.Join(',',
                trial.TrialIndex.ToString(CultureInfo.InvariantCulture),
                FormatString(trial.TaskName),
                trial.ScenarioName,
                trial.TargetLabel,
                FormatString(trial.ClassLabel),
                FormatNullableBool(trial.IsPositiveClass),
                trial.SourceType,
                trial.DetectorName,
                trial.DetectorMode,
                FormatString(trial.ScoreOrientation),
                FormatNullable(trial.ConditionSnrDb),
                FormatNullable(trial.SourceSnrDb),
                trial.WindowLength.ToString(CultureInfo.InvariantCulture),
                trial.WindowCount.ToString(CultureInfo.InvariantCulture),
                trial.SampleCount.ToString(CultureInfo.InvariantCulture),
                trial.StartIndex.ToString(CultureInfo.InvariantCulture),
                trial.Score.ToString("F6", CultureInfo.InvariantCulture),
                trial.IsAboveThreshold ? "true" : "false",
                trial.MeanSample.ToString("F6", CultureInfo.InvariantCulture),
                trial.PeakSample.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public void WriteSummary(string path, IReadOnlyList<SummaryRecord> summaries)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskName,scenarioName,targetLabel,detectorName,detectorMode,scoreOrientation,conditionSnrDb,sourceSnrDb,windowLength,count,positiveCount,negativeCount,minScore,maxScore,meanScore,standardDeviation,aboveThresholdCount,auc");

        foreach (var summary in summaries)
        {
            writer.WriteLine(string.Join(',',
                FormatString(summary.TaskName),
                FormatString(summary.ScenarioName),
                FormatString(summary.TargetLabel),
                summary.DetectorName,
                summary.DetectorMode,
                FormatString(summary.ScoreOrientation),
                FormatNullable(summary.ConditionSnrDb),
                FormatNullable(summary.SourceSnrDb),
                FormatNullable(summary.WindowLength),
                summary.Count.ToString(CultureInfo.InvariantCulture),
                FormatNullable(summary.PositiveCount),
                FormatNullable(summary.NegativeCount),
                summary.MinScore.ToString("F6", CultureInfo.InvariantCulture),
                summary.MaxScore.ToString("F6", CultureInfo.InvariantCulture),
                summary.MeanScore.ToString("F6", CultureInfo.InvariantCulture),
                summary.StandardDeviation.ToString("F6", CultureInfo.InvariantCulture),
                summary.AboveThresholdCount.ToString(CultureInfo.InvariantCulture),
                FormatNullable(summary.Auc)));
        }
    }

    public void WriteRocPoints(string path, IReadOnlyList<RocPointRecord> rocPoints)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskName,detectorName,detectorMode,scoreOrientation,conditionSnrDb,windowLength,threshold,tpr,fpr,truePositives,falsePositives,positiveCount,negativeCount,auc");

        foreach (var point in rocPoints)
        {
            writer.WriteLine(string.Join(',',
                point.TaskName,
                point.DetectorName,
                point.DetectorMode,
                point.ScoreOrientation,
                FormatNullable(point.ConditionSnrDb),
                point.WindowLength.ToString(CultureInfo.InvariantCulture),
                FormatNullable(point.Threshold),
                point.TruePositiveRate.ToString("F6", CultureInfo.InvariantCulture),
                point.FalsePositiveRate.ToString("F6", CultureInfo.InvariantCulture),
                point.TruePositives.ToString(CultureInfo.InvariantCulture),
                point.FalsePositives.ToString(CultureInfo.InvariantCulture),
                point.PositiveCount.ToString(CultureInfo.InvariantCulture),
                point.NegativeCount.ToString(CultureInfo.InvariantCulture),
                point.Auc.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    private static string FormatString(string? value) => value ?? string.Empty;

    private static string FormatNullable(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("F6", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string FormatNullable(int? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string FormatNullableBool(bool? value)
    {
        return value.HasValue
            ? (value.Value ? "true" : "false")
            : string.Empty;
    }
}
