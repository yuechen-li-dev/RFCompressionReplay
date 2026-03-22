namespace RfCompressionReplay.Core.Execution;

public sealed class GitCommitResolver
{
    public string Resolve(string workingDirectory)
    {
        var headPath = Path.Combine(workingDirectory, ".git", "HEAD");
        if (!File.Exists(headPath))
        {
            return "unknown";
        }

        var headContent = File.ReadAllText(headPath).Trim();
        if (headContent.StartsWith("ref: ", StringComparison.Ordinal))
        {
            var relativeRef = headContent[5..];
            var refPath = Path.Combine(workingDirectory, ".git", relativeRef.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : "unknown";
        }

        return string.IsNullOrWhiteSpace(headContent) ? "unknown" : headContent;
    }
}
