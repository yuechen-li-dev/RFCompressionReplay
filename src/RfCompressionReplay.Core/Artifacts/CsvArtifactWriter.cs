using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Artifacts;

public sealed class CsvArtifactWriter
{
    public void WriteTrials(string path, IReadOnlyList<TrialRecord> trials)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("trialIndex,detectorName,detectorMode,windowCount,sampleCount,score,isAboveThreshold,meanSample,peakSample");

        foreach (var trial in trials)
        {
            writer.WriteLine(string.Join(',',
                trial.TrialIndex.ToString(CultureInfo.InvariantCulture),
                trial.DetectorName,
                trial.DetectorMode,
                trial.WindowCount.ToString(CultureInfo.InvariantCulture),
                trial.SampleCount.ToString(CultureInfo.InvariantCulture),
                trial.Score.ToString("F6", CultureInfo.InvariantCulture),
                trial.IsAboveThreshold ? "true" : "false",
                trial.MeanSample.ToString("F6", CultureInfo.InvariantCulture),
                trial.PeakSample.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }
}
