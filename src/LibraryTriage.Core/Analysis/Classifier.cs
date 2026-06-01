using LibraryTriage.Core.Models;
using System.Linq;
using System.IO;

namespace LibraryTriage.Core.Analysis;

public class Classifier
{
    private readonly Settings _settings;

    public Classifier(Settings settings)
    {
        _settings = settings;
    }

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
            Reasoning = string.Join(", ", reasoning),
            ShowName = ParseShowName(file.FilePath),
            Season = ParseSeason(file.FilePath),
            DisplayName = CleanEpisodeName(Path.GetFileName(file.FilePath))
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
        if (file.Height < _settings.Classification.MinHeightForSR)
        {
            srSignalScore += 4;
            reasoning.Add($"Low Resolution ({file.Height}) detected");
        }
        if (file.BitRateDensity <= _settings.Classification.H264BitrateDensityThresholdLower && file.CodecName == "h264")
        {
            srSignalScore += 3;
            reasoning.Add("h264 with low bitrate density");
        }
        //(this was the year the first ever film was made, and when ER changed to HD, respectively, see ReadME)
        int seasonNumber = ParseSeasonNumber(file.FilePath);
        int effectiveYear = (file.YearReleased > 0 && seasonNumber > 1) 
            ? file.YearReleased + seasonNumber 
            : file.YearReleased;

        if (effectiveYear >= 1888 && effectiveYear < _settings.Classification.YearThresholdLower)
        {
            srSignalScore += 2;
            reasoning.Add($"Produced pre-2000 (effective year: {effectiveYear})");
        }
        else if (effectiveYear >= _settings.Classification.YearThresholdLower && effectiveYear <= _settings.Classification.YearThresholdUpper)
        {
            srSignalScore += 1;
            reasoning.Add($"Transitional era content (effective year: {effectiveYear})");
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
        if (file.CodecName == "h264" && file.BitRateDensity >= _settings.Classification.H264BitrateDensityThresholdHigher)
        {
            reasoning.Add($"Inefficient H264 encode (bitrate density: {file.BitRateDensity:F3})");
            reencode = true;
        }
        if (file.CodecName == "hevc" && file.BitRateDensity > _settings.Classification.H265BitrateDensityThreshold)
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
        if (file.CodecName == "h264" && file.BitRateDensity >= _settings.Classification.H264BitrateDensityThresholdHigher)
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

    private string ParseSeason(string filePath)
    {
        var parts = filePath.Split(Path.DirectorySeparatorChar);
        
        int categoryIndex = Array.FindIndex(parts, p => p.Contains("Shows"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Movies"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Shorts"));
        
        if (categoryIndex == -1 || categoryIndex + 2 >= parts.Length)
            return string.Empty;

        // only return show name for TV shows
        if (parts[categoryIndex].Contains("Shows"))
            return parts[categoryIndex + 2];
            
        return string.Empty;
    }

    private int ParseSeasonNumber(string filePath)
    {
        var parts = filePath.Split(Path.DirectorySeparatorChar);
        
        int categoryIndex = Array.FindIndex(parts, p => p.Contains("Shows"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Movies"));
        if (categoryIndex == -1)
            categoryIndex = Array.FindIndex(parts, p => p.Contains("Shorts"));
        
        if (categoryIndex == -1 || categoryIndex + 2 >= parts.Length)
            return 0;

        if (!parts[categoryIndex].Contains("Shows"))
            return 0;

        var season = parts[categoryIndex + 2];
        var seasonParts = season.Split(' ');
        
        if (seasonParts.Length < 2)
            return 0;

        return int.TryParse(seasonParts[1], out int number) ? number : 0;
    }

    // for HTML output
    private string CleanEpisodeName(string fileName)
    {
        // check for a good cutoff in name string before cleaning
        var noiseMarkers = new[] {
            "1080p", "720p", "480p", "2160p", "4k",
            "WEB-DL", "WEBRip", "BluRay", "BDRip", "HDTV", "DVDRip",
            "x264", "x265", "H.264", "H.265", "HEVC", "AVC",
            "AAC", "DTS", "AC3"
        };

        // match against S--E-- portion of filename
        var match = Regex.Match(fileName, @"[Ss](\d{1,2})[Ee](\d{1,2})");
        if (!match.Success)
            return fileName;

        int season = int.Parse(match.Groups[1].Value);
        int episode = int.Parse(match.Groups[2].Value);

        // index cutoff point
        int afterMatchPos = match.Index + match.Length;
        int cutoffPos = fileName.Length;

        foreach (var marker in noiseMarkers)
        {
            int markerPos = fileName.IndexOf(marker, afterMatchPos, StringComparison.OrdinalIgnoreCase);
            if (markerPos != -1 && markerPos < cutoffPos)
            {
                cutoffPos = markerPos;
            }
        }

        string rawTitle = fileName.Substring(afterMatchPos, cutoffPos - afterMatchPos);

        string cleanTitle = rawTitle
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Trim(' ', '-');

        // assemble formatted output
        if (string.IsNullOrWhiteSpace(cleanTitle))
            return $"S{season:D2}E{episode:D2}";

        return $"S{season:D2}E{episode:D2} — {cleanTitle}";
    }
}