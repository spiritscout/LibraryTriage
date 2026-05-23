using System.IO;
using LibraryTriage.Core.Discovery;
using LibraryTriage.Core.FFprobe;
using LibraryTriage.Core.Models;
using LibraryTriage.Core.Analysis;
using LibraryTriage.Core.Output;

// no args check
if (args.Length == 0)
{
    Console.WriteLine("Please provide a library path as an argument.");
    return;
}

// multiple args check
if (args.Length > 1)
{
    Console.WriteLine("Please provide a single path.");
    return;
}

var libraryPath = args[0];

// initialise classes for processing loop
var fileDiscovery = new FileDiscovery();
var ffprobeRunner = new FFprobeRunner();
var classifier = new Classifier();
var results = new List<ClassificationResult>();
var reportWriter = new ReportWriter();

Console.WriteLine($"Scanning library at: {libraryPath}");

var files = fileDiscovery.DiscoverFiles(libraryPath);

Console.WriteLine($"Found {files.Count} video files");

//main processing loop
foreach (var file in files)
{
    try
    {
        var ffprobeOutput = await ffprobeRunner.RunAsync(file);
        var mediaFile = new MediaFile(ffprobeOutput, file);
        var result = classifier.Classify(mediaFile);
        results.Add(result);
        Console.WriteLine($"Processed: {Path.GetFileName(file)} — {string.Join(", ", result.Recommendations)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {file}: {ex.Message}");
    }
}

// output results
Console.WriteLine($"Scan complete. {results.Count} files processed.");

var outputPath = Path.Combine(libraryPath, "triage_report.json");
reportWriter.WriteReport(results, outputPath);

Console.WriteLine($"Report written to: {outputPath}");