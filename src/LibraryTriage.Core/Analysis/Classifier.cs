using LibraryTriage.Core.Models;
using System.Linq;

namespace LibraryTriage.Core.Analysis;

public class Classifier
{
    public ClassificationResult Classify(MediaFile file)
    {
        var recommendations = new List<RecommendationType>();
        var reasoning = new List<string>();
        int srSignalScore = 0;

        //helper method calls
        EvaluateSRCandidate(file, recommendations, reasoning, ref srSignalScore);
        EvaluateReencode(file, recommendations, reasoning);
        EvaluateH265Upgrade(file, recommendations, reasoning);
        EvaluateAlreadyH265(file, recommendations, reasoning);

        //if no recommendations add LeaveAlone tag
        if (!recommendations.Any())
            recommendations.Add(RecommendationType.LeaveAlone);

        var confidence = srSignalScore >= 8 ? ConfidenceLevel.High
            : srSignalScore >= 4 ? ConfidenceLevel.Medium
            : ConfidenceLevel.Low;

        return new ClassificationResult
        {
            FilePath = file.FilePath,
            Recommendations = recommendations,
            Confidence = confidence,
            Reasoning = string.Join(", ", reasoning)
        };
    }
}