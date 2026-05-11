using LibraryTriage.Core.FFprobe;
using System.Linq;

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
    }
}