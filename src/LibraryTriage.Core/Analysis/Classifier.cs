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

    private void EvaluateSRCandidate(MediaFile file, List<RecommendationType> recommendations, List<string> reasoning, ref int srSignalScore)
    {
        var oldCodecs = new[] { "mpeg2video", "msmpeg4v3", "xvid", "wmv2", "wmv3", "vc1" };
        if (oldCodecs.Contains(file.CodecName))
        {
            srSignalScore += 4;
            reasoning.Add($"Old codec ({file.CodecName}) detected");
        }
        if (file.Height < 720)
        {
            srSignalScore += 4;
            reasoning.Add($"Low Resolution ({file.Height}) detected");
        }
        if (file.BitRateDensity <= 0.03 && file.CodecName == "h264")
        {
            srSignalScore += 3;
            reasoning.Add("h264 with low bitrate density");
        }
        //(this was the year ER changed to HD)
        if (file.YearReleased <= 2008)
        {
            srSignalScore += 2;
            reasoning.Add("Produced pre-2008");
        }
        //add a codec advisory, no scoring effect
        reasoning.Add($"Encoder: {file.Encoder}");
        
        //add the recommendation 
        if (srSignalScore > 0)
            recommendations.Add(RecommendationType.SRCandidate);
    }
}