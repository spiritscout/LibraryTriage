using LibraryTriage.Core.FFprobe;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryTriage.Core.Models;

public class MediaFile
{
    public string CodecName { get; set; }
    public string FilePath { get; set; }
    public string Encoder { get; set; }
    public string CreationTime { get; set; }
    public string Category { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int YearReleased { get; set; }
    public long BitRate { get; set; }
    public long Size { get; set; }
    public double AvgFrameRate { get; set; }
    public double Duration { get; set; }
    public double BitRateDensity { get; set; }

    public MediaFile(FFprobeOutput ffprobeOutput, string filePath)
    {
        var videoStream = ffprobeOutput.Streams.FirstOrDefault(s => s.CodecType == "video");
        if (videoStream == null)
        {
            throw new InvalidOperationException($"No video stream found in given file: {filePath}");
        }
        FilePath = filePath;
        CodecName = videoStream.CodecName;
        Width = videoStream.Width;
        Height = videoStream.Height;
        Size = long.Parse(ffprobeOutput.Format.Size);
        Encoder = ffprobeOutput.Format.Tags?.Encoder ?? "Unknown";
        CreationTime = ffprobeOutput.Format.Tags?.CreationTime ?? "Unknown";
        BitRate = long.Parse(ffprobeOutput.Format.BitRate);
        Duration = double.Parse(ffprobeOutput.Format.Duration);
        AvgFrameRate = ParseFrameRate(videoStream.AvgFrameRate);
        BitRateDensity = BitRate / ((double)Width * Height * AvgFrameRate);
        YearReleased = ParseYear(filePath);
        Category = ParseCategory(filePath);
        

    }

    private double ParseFrameRate(string frameRateString)
    {
        var frameRateArr = frameRateString.Split('/');
        return double.Parse(frameRateArr[0]) / double.Parse(frameRateArr[1]);
    }

    private int ParseYear(string filePath)
    {
        var match = Regex.Match(filePath, @"\((\d{4})\)");
        if (!match.Success)
            return 0;

        int year = int.Parse(match.Groups[1].Value);
        if (year >= 1888 && year <= DateTime.Now.Year)
            return year;

        return 0;
    }

    private string ParseCategory(string filePath)
    {
        if (filePath.Contains("Show")){
            return "Show";
        } else if (filePath.Contains("Movie")){
            return "Movie";
        } else if (filePath.Contains("Short")){
            return "Short";
        } else
        {
            return "other";
        }
    }
}