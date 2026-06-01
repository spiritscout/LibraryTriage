namespace LibraryTriage.Core.Models;

public enum RecommendationType
{
    LeaveAlone,
    SRCandidate,
    ReencodeRecommended,
    H265UpgradeRecommended,
    AlreadyH265
}

public enum ConfidenceLevel
{
    High,
    Medium,
    Low
}

public class ClassificationResult
{
    public string FilePath { get; set; }
    public string Category { get; set; }
    public string ShowName { get; set; }
    public string Season { get; set; }
    public string FileName { get; set; }
    public string Resolution { get; set; }
    public double MegabytesPerMinute { get; set; }
    public double FileSizeMB { get; set; }
    public string Codec { get; set; }
    public List<RecommendationType> Recommendations { get; set; }
    public ConfidenceLevel? Confidence { get; set; }
    public string Reasoning { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}