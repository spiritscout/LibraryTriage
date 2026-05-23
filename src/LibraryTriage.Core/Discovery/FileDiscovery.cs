using System.IO;

namespace LibraryTriage.Core.Discovery;

public class FileDiscovery
{
    private static readonly string[] VideoExtensions = 
        { ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".mpg", ".mpeg", ".ts", ".m2ts", ".webm" };

    public List<string> DiscoverFiles(string rootPath)
    {
        //recursively walk a directory, return all file paths
        var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
        var videoFiles = new List<string>();
        //loop through and add files to new list if extension matches
        foreach (var file in allFiles)
        {
            if (VideoExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                videoFiles.Add(file);
            }
        }
        return videoFiles;

    }
}