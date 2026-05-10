namespace LibraryTriage.Core.FFprobe;

public class FFprobeOutput
{
    public List<FFprobeStream> Streams { get; set; }
    public FFprobeFormat Format { get; set; }
}

public class FFprobeStream
{
    public string CodecName { get; set; }
    public string CodecType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string AvgFrameRate { get; set; }
    public string BitRate { get; set; }
}

public class FFprobeFormat
{
    public string Filename { get; set; }
    public string Duration { get; set; }
    public string Size { get; set; }
    public string BitRate { get; set; }
    public FFprobeTags Tags { get; set; }
}

public class FFprobeTags
{
    public string Encoder { get; set; }
    public string CreationTime { get; set; }
}
