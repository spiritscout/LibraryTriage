namespace LibraryTriage.Core.Models;

public class Settings
{
    public ClassificationSettings Classification { get; set; } = new();
    public DiscoverySettings Discovery { get; set; } = new();
    public OutputSettings Output { get; set; } = new();

    public class ClassificationSettings
    {
        public int YearThresholdLower { get; set; }
        public int YearThresholdUpper { get; set; }
        public int MinHeightForSR { get; set; }
        public double H264BitrateDensityThresholdLower { get; set; }
        public double H264BitrateDensityThresholdHigher { get; set; }
        public double H265BitrateDensityThreshold { get; set; }
    }

    public class DiscoverySettings
    {
        public string[] VideoExtensions { get; set; } = Array.Empty<string>();
    }

    public class OutputSettings
    {
        public bool AutoOpenReport { get; set; }
    }
}