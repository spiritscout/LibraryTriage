using LibraryTriage.Core.Models;
using System.Linq;
using System.IO;

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

        ConfidenceLevel? confidence = recommendations.Contains(RecommendationType.SRCandidate)
            ? srSignalScore >= 8 ? ConfidenceLevel.High
                : srSignalScore >= 4 ? ConfidenceLevel.Medium
                : ConfidenceLevel.Low
            : null;

        return new ClassificationResult
        {
            FilePath = file.FilePath,
            FileName = Path.GetFileName(file.FilePath),
            Resolution = $"{file.Width}x{file.Height}",
            FileSizeMB = Math.Round(file.Size / 1024.0 / 1024.0, 1),
            Category = file.Category,
            MegabytesPerMinute = file.MegabytesPerMinute,
            Codec = file.CodecName,
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
        //(this was the year the first ever film was made, and when ER changed to HD, respectively)
        if (file.YearReleased >= 1888 && file.YearReleased <= 2008)
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

    private void EvaluateReencode(MediaFile file, List<RecommendationType> recommendations, List<string> reasoning)
    {   
        var reencode = false;
        if (file.CodecName == "h264" && file.BitRateDensity >= 0.1)
        {
            reasoning.Add($"Inefficient H264 encode (bitrate density: {file.BitRateDensity:F3})");
            reencode = true;
        }
        if (file.CodecName == "hevc" && file.BitRateDensity > 0.06)
        {
            reasoning.Add($"Inefficient H265 encode (bitrate density: {file.BitRateDensity:F3})");
            reencode = true;
        }
        if (reencode)
        {
            recommendations.Add(RecommendationType.ReencodeRecommended);
        }
    }

    private void EvaluateH265Upgrade(MediaFile file, List<RecommendationType> recommendations, List<string> reasoning)
    {
        if (file.CodecName == "h264" && file.BitRateDensity >= 0.1)
        {
            reasoning.Add($"Could upgrade to H265");
            recommendations.Add(RecommendationType.H265UpgradeRecommended);
        }
    }

    private void EvaluateAlreadyH265(MediaFile file, List<RecommendationType> recommendations, List<string> reasoning)
    {
        if (file.CodecName == "hevc")
        {
            reasoning.Add("Already encoded in H265");
            recommendations.Add(RecommendationType.AlreadyH265);
        }
    }

    private string ParseShowName(string filePath)
    {
        var parts = filePath.Split(Path.DirectorySeparatorChar);
        
        int categoryIndex = Array.FindIndex(parts, p => p.Contains("Shows"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Movies"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Shorts"));
        
        if (categoryIndex == -1 || categoryIndex + 1 >= parts.Length)
            return string.Empty;

        // only return show name for TV shows
        if (parts[categoryIndex].Contains("Shows"))
            return parts[categoryIndex + 1];
            
        return string.Empty;
    }
}