//contains Process for spawning and controlling FFprobe
using System.Diagnostics;

//parent that contains deserializer
using System.Text.Json;

namespace LibraryTriage.Core.FFprobe;

public class FFprobeRunner
{
    public async Task<FFprobeOutput> RunAsync(string filePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v quiet -print_format json -show_streams -show_format \"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        string json = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return JsonSerializer.Deserialize<FFprobeOutput>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}