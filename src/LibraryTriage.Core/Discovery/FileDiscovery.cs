using System.IO;
using LibraryTriage.Core.Models;

namespace LibraryTriage.Core.Discovery;

public class FileDiscovery
{
    private readonly Settings _settings;

    public FileDiscovery(Settings settings)
    {
        _settings = settings;
    }

    public List<string> DiscoverFiles(string rootPath)
    {
        //recursively walk a directory, return all file paths
        var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
        var videoFiles = new List<string>();
        //loop through and add files to new list if extension matches
        foreach (var file in allFiles)
        {
            if (_settings.Discovery.VideoExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                videoFiles.Add(file);
            }
        }
        return videoFiles;
    }
}