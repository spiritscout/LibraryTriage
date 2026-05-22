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
    public List<RecommendationType> Recommendations { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public string Reasoning { get; set; }
}