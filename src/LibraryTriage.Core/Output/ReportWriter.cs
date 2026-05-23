using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryTriage.Core.Models;

namespace LibraryTriage.Core.Output;

public class ReportWriter
{
    public void WriteReport(List<ClassificationResult> results, string outputPath)
    {
        var groupedResults = results.GroupBy(r => r.Category);
        var report = new Dictionary<string, List<ClassificationResult>>();
        foreach (var group in groupedResults)
        {
            report[group.Key] = group.ToList();
        }
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        });
        File.WriteAllText(outputPath, json);
    }
}