//used for attribute that converts snake_case to PascalCase
using System.Text.Json.Serialization;

namespace LibraryTriage.Core.FFprobe;

public class FFprobeOutput
{
    [JsonPropertyName("streams")]
    public List<FFprobeStream> Streams { get; set; }
    [JsonPropertyName("format")]
    public FFprobeFormat Format { get; set; }
}

public class FFprobeStream
{
    [JsonPropertyName("codec_name")]
    public string CodecName { get; set; }
    [JsonPropertyName("codec_type")]
    public string CodecType { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("avg_frame_rate")]
    public string AvgFrameRate { get; set; }
    [JsonPropertyName("bit_rate")]
    public string BitRate { get; set; }
}

public class FFprobeFormat
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; }
    [JsonPropertyName("duration")]
    public string Duration { get; set; }
    [JsonPropertyName("size")]
    public string Size { get; set; }
    [JsonPropertyName("bit_rate")]
    public string BitRate { get; set; }
    [JsonPropertyName("tags")]
    public FFprobeTags Tags { get; set; }
}

public class FFprobeTags
{
    [JsonPropertyName("encoder")]
    public string Encoder { get; set; }
    [JsonPropertyName("creation_time")]
    public string CreationTime { get; set; }
}
