using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using LibraryTriage.Core.Models;

namespace LibraryTriage.Core.Output;

public class ReportWriter
{
    public void WriteReport(List<ClassificationResult> results, string outputPath)
    {
        // filter out leave alone and already h265
        var actionable = results.Where(r =>
            !r.Recommendations.Contains(RecommendationType.LeaveAlone) &&
            !r.Recommendations.Contains(RecommendationType.AlreadyH265)
        ).ToList();

        var dataJson = BuildDataJson(actionable);
        var html = GetHtmlTemplate(dataJson);
        File.WriteAllText(outputPath.Replace(".json", ".html"), html);
    }

    private string BuildDataJson(List<ClassificationResult> results)
    {
        var films = results.Where(r => r.Category.Contains("Movies")).ToList();
        var shows = results.Where(r => r.Category.Contains("Shows")).ToList();
        var shorts = results.Where(r => r.Category.Contains("Shorts")).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"Films\": {BuildFlatArrayJson(films)},");
        sb.AppendLine($"  \"TV Shows\": {BuildShowsJson(shows)},");
        sb.AppendLine($"  \"Short Films\": {BuildFlatArrayJson(shorts)}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string BuildFlatArrayJson(List<ClassificationResult> results)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(results, options);
    }

    private string BuildShowsJson(List<ClassificationResult> results)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var sb = new StringBuilder();
        sb.AppendLine("{");

        var showGroups = results.GroupBy(r => r.ShowName).ToList();

        for (int i = 0; i < showGroups.Count; i++)
        {
            var show = showGroups[i];
            sb.AppendLine($"  \"{show.Key}\": {{");

            var seasonGroups = show.GroupBy(r => r.Season).ToList();

            for (int j = 0; j < seasonGroups.Count; j++)
            {
                var season = seasonGroups[j];
                var episodesJson = JsonSerializer.Serialize(season.ToList(), options);
                var comma = j < seasonGroups.Count - 1 ? "," : "";
                sb.AppendLine($"    \"{season.Key}\": {episodesJson}{comma}");
            }

            var showComma = i < showGroups.Count - 1 ? "," : "";
            sb.AppendLine($"  }}{showComma}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GetHtmlTemplate(string dataJson)
    {
        var beforeData = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Library Triage Report</title>
<style>
  :root {
    --bg: #0f0f0f;
    --surface: #181818;
    --surface2: #222222;
    --border: #2e2e2e;
    --text: #e8e8e0;
    --muted: #888880;
    --accent: #c8c0a0;
    --red-high: #c0392b;
    --red-high-bg: #2a1210;
    --amber-mid: #d4820a;
    --amber-mid-bg: #2a1e08;
    --yellow-low: #a89020;
    --yellow-low-bg: #232010;
    --blue: #2980b9;
    --blue-bg: #0c1e2a;
    --purple: #7c5cbf;
    --purple-bg: #1a1228;
    --font-mono: 'Courier New', Courier, monospace;
  }

  * { box-sizing: border-box; margin: 0; padding: 0; }

  body {
    background: var(--bg);
    color: var(--text);
    font-family: 'Segoe UI', system-ui, sans-serif;
    font-size: 14px;
    line-height: 1.5;
    padding: 2rem;
  }

  header {
    border-bottom: 1px solid var(--border);
    padding-bottom: 1.5rem;
    margin-bottom: 1.5rem;
  }

  header h1 {
    font-size: 1.1rem;
    font-weight: 500;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--accent);
    margin-bottom: 0.25rem;
  }

  header p {
    color: var(--muted);
    font-size: 0.8rem;
    font-family: var(--font-mono);
  }

  .summary-bar {
    display: flex;
    gap: 1.5rem;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
  }

  .summary-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .summary-item .label {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--muted);
  }

  .summary-item .value {
    font-size: 1.4rem;
    font-weight: 500;
    font-family: var(--font-mono);
    color: var(--text);
  }

  .controls {
    display: flex;
    gap: 0.75rem;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
    align-items: center;
  }

  .search-input {
    background: var(--surface);
    border: 1px solid var(--border);
    color: var(--text);
    padding: 0.4rem 0.75rem;
    font-size: 0.85rem;
    font-family: inherit;
    border-radius: 3px;
    width: 260px;
    outline: none;
  }

  .search-input:focus {
    border-color: var(--accent);
  }

  .filter-btn {
    background: var(--surface);
    border: 1px solid var(--border);
    color: var(--muted);
    padding: 0.4rem 0.75rem;
    font-size: 0.75rem;
    font-family: inherit;
    border-radius: 3px;
    cursor: pointer;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    transition: all 0.1s;
  }

  .filter-btn:hover, .filter-btn.active {
    border-color: var(--accent);
    color: var(--accent);
  }

  .category-section {
    margin-bottom: 1.5rem;
    border: 1px solid var(--border);
    border-radius: 4px;
    overflow: hidden;
  }

  .category-header {
    background: var(--surface);
    padding: 0.75rem 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    user-select: none;
    border-bottom: 1px solid var(--border);
  }

  .category-header:hover {
    background: var(--surface2);
  }

  .category-header h2 {
    font-size: 0.8rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--accent);
  }

  .category-header .count {
    font-size: 0.75rem;
    color: var(--muted);
    font-family: var(--font-mono);
  }

  .chevron {
    color: var(--muted);
    font-size: 0.7rem;
    transition: transform 0.15s;
  }

  .chevron.open { transform: rotate(180deg); }

  .category-body { display: none; }
  .category-body.open { display: block; }

  .show-group {
    border-bottom: 1px solid var(--border);
  }

  .show-group:last-child { border-bottom: none; }

  .show-header {
    padding: 0.6rem 1rem 0.6rem 1.25rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    user-select: none;
    background: var(--surface);
  }

  .show-header:hover { background: var(--surface2); }

  .show-header h3 {
    font-size: 0.8rem;
    font-weight: 500;
    color: var(--text);
  }

  .show-body { display: none; }
  .show-body.open { display: block; }

  .season-group {
    border-bottom: 1px solid var(--border);
  }

  .season-group:last-child { border-bottom: none; }

  .season-header {
    padding: 0.5rem 1rem 0.5rem 2rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    user-select: none;
    background: var(--bg);
  }

  .season-header:hover { background: var(--surface); }

  .season-header h4 {
    font-size: 0.75rem;
    font-weight: 400;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.08em;
  }

  .season-body { display: none; }
  .season-body.open { display: block; }

  .file-entry {
    padding: 0.65rem 1rem 0.65rem 3rem;
    border-bottom: 1px solid var(--border);
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    background: var(--bg);
  }

  .file-entry:last-child { border-bottom: none; }

  .file-entry.film-entry {
    padding-left: 1.5rem;
  }

  .file-entry-top {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 1rem;
  }

  .file-name {
    font-size: 0.8rem;
    color: var(--text);
    word-break: break-word;
  }

  .badges {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
    flex-shrink: 0;
  }

  .badge {
    font-size: 0.65rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    padding: 2px 7px;
    border-radius: 2px;
    white-space: nowrap;
  }

  .badge-sr-high { background: var(--red-high-bg); color: #e06050; border: 1px solid var(--red-high); }
  .badge-sr-mid  { background: var(--amber-mid-bg); color: #d4920a; border: 1px solid var(--amber-mid); }
  .badge-sr-low  { background: var(--yellow-low-bg); color: #a89020; border: 1px solid var(--yellow-low); }
  .badge-reencode { background: var(--blue-bg); color: #4a9fd4; border: 1px solid var(--blue); }
  .badge-h265    { background: var(--purple-bg); color: #9a7cd4; border: 1px solid var(--purple); }

  .file-meta {
    display: flex;
    gap: 1.25rem;
    flex-wrap: wrap;
  }

  .meta-item {
    font-size: 0.72rem;
    font-family: var(--font-mono);
    color: var(--muted);
  }

  .meta-item span {
    color: var(--text);
    margin-left: 0.25rem;
  }

  .reasoning {
    font-size: 0.72rem;
    color: var(--muted);
    font-style: italic;
  }

  .confidence {
    font-size: 0.7rem;
    font-family: var(--font-mono);
    color: var(--muted);
  }

  .no-results {
    padding: 2rem;
    text-align: center;
    color: var(--muted);
    font-size: 0.85rem;
  }
</style>
</head>
<body>

<header>
  <h1>Library Triage Report</h1>
  <p id="report-meta">Generated</p>
</header>

<div class="summary-bar">
  <div class="summary-item">
    <span class="label">Total flagged</span>
    <span class="value" id="total-count">0</span>
  </div>
  <div class="summary-item">
    <span class="label">SR candidates</span>
    <span class="value" id="sr-count">0</span>
  </div>
  <div class="summary-item">
    <span class="label">Re-encode</span>
    <span class="value" id="reencode-count">0</span>
  </div>
  <div class="summary-item">
    <span class="label">H265 upgrade</span>
    <span class="value" id="h265-count">0</span>
  </div>
</div>

<div class="controls">
  <input class="search-input" type="text" placeholder="Search filename..." id="search-input" />
  <button class="filter-btn active" data-filter="all">All</button>
  <button class="filter-btn" data-filter="SRCandidate">SR candidate</button>
  <button class="filter-btn" data-filter="ReencodeRecommended">Re-encode</button>
  <button class="filter-btn" data-filter="H265UpgradeRecommended">H265 upgrade</button>
</div>

<div id="report-container"></div>
<div class="no-results" id="no-results" style="display:none">No results match your search or filter.</div>

<script>
const data = 
""";

        var afterData = """
;

let activeFilter = "all";
let searchQuery = "";

function getBadgeHtml(recommendations, confidence) {
  return recommendations.map(r => {
    if (r === "SRCandidate") {
      const level = confidence ? confidence.toLowerCase() : "low";
      const cls = level === "high" ? "badge-sr-high" : level === "medium" ? "badge-sr-mid" : "badge-sr-low";
      const label = `SR candidate \u2014 ${confidence || "Low"}`;
      return `<span class="badge ${cls}">${label}</span>`;
    }
    if (r === "ReencodeRecommended") return `<span class="badge badge-reencode">Re-encode</span>`;
    if (r === "H265UpgradeRecommended") return `<span class="badge badge-h265">H265 upgrade</span>`;
    return "";
  }).join("");
}

function fileMatchesFilter(file) {
  if (activeFilter !== "all" && !file.Recommendations.includes(activeFilter)) return false;
  if (searchQuery && !file.FileName.toLowerCase().includes(searchQuery)) return false;
  return true;
}

function fileEntryHtml(file, extraClass) {
  const badges = getBadgeHtml(file.Recommendations, file.Confidence);
  const confidenceHtml = file.Confidence
    ? `<div class="confidence">Confidence: ${file.Confidence}</div>`
    : "";
  return `
    <div class="file-entry ${extraClass || ""}" data-recs="${file.Recommendations.join(",")}" data-name="${file.FileName.toLowerCase()}">
      <div class="file-entry-top">
        <div class="file-name">${file.FileName}</div>
        <div class="badges">${badges}</div>
      </div>
      <div class="file-meta">
        <div class="meta-item">Codec<span>${file.Codec.toUpperCase()}</span></div>
        <div class="meta-item">Resolution<span>${file.Resolution}</span></div>
        <div class="meta-item">Size<span>${file.FileSizeMB} MB</span></div>
        <div class="meta-item">MB/min<span>${file.MegabytesPerMinute.toFixed(1)}</span></div>
      </div>
      ${confidenceHtml}
      <div class="reasoning">${file.Reasoning}</div>
    </div>`;
}

function buildReport() {
  const container = document.getElementById("report-container");
  container.innerHTML = "";

  let totalVisible = 0;
  let srVisible = 0;
  let reencodeVisible = 0;
  let h265Visible = 0;

  const categories = [
    { key: "Films", label: "Films", type: "flat" },
    { key: "TV Shows", label: "TV Shows", type: "nested" },
    { key: "Short Films", label: "Short Films", type: "flat" }
  ];

  categories.forEach(cat => {
    const catData = data[cat.key];
    if (!catData) return;

    let catHtml = "";
    let catCount = 0;

    if (cat.type === "flat") {
      catData.forEach(file => {
        if (!fileMatchesFilter(file)) return;
        catHtml += fileEntryHtml(file, "film-entry");
        catCount++;
        totalVisible++;
        if (file.Recommendations.includes("SRCandidate")) srVisible++;
        if (file.Recommendations.includes("ReencodeRecommended")) reencodeVisible++;
        if (file.Recommendations.includes("H265UpgradeRecommended")) h265Visible++;
      });
    } else {
      Object.entries(catData).forEach(([showName, seasons]) => {
        let showHtml = "";
        let showCount = 0;

        Object.entries(seasons).forEach(([seasonName, episodes]) => {
          let seasonHtml = "";
          let seasonCount = 0;

          episodes.forEach(file => {
            if (!fileMatchesFilter(file)) return;
            seasonHtml += fileEntryHtml(file, "");
            seasonCount++;
            showCount++;
            catCount++;
            totalVisible++;
            if (file.Recommendations.includes("SRCandidate")) srVisible++;
            if (file.Recommendations.includes("ReencodeRecommended")) reencodeVisible++;
            if (file.Recommendations.includes("H265UpgradeRecommended")) h265Visible++;
          });

          if (seasonCount > 0) {
            showHtml += `
              <div class="season-group">
                <div class="season-header" onclick="toggleSection(this)">
                  <h4>${seasonName}</h4>
                  <div style="display:flex;gap:0.75rem;align-items:center">
                    <span class="count">${seasonCount} items</span>
                    <span class="chevron open">\u25b2</span>
                  </div>
                </div>
                <div class="season-body open">${seasonHtml}</div>
              </div>`;
          }
        });

        if (showCount > 0) {
          catHtml += `
            <div class="show-group">
              <div class="show-header" onclick="toggleSection(this)">
                <h3>${showName}</h3>
                <div style="display:flex;gap:0.75rem;align-items:center">
                  <span class="count">${showCount} items</span>
                  <span class="chevron open">\u25b2</span>
                </div>
              </div>
              <div class="show-body open">${showHtml}</div>
            </div>`;
        }
      });
    }

    if (catCount > 0) {
      container.innerHTML += `
        <div class="category-section">
          <div class="category-header" onclick="toggleSection(this)">
            <h2>${cat.label}</h2>
            <div style="display:flex;gap:0.75rem;align-items:center">
              <span class="count">${catCount} items</span>
              <span class="chevron open">\u25b2</span>
            </div>
          </div>
          <div class="category-body open">${catHtml}</div>
        </div>`;
    }
  });

  document.getElementById("total-count").textContent = totalVisible;
  document.getElementById("sr-count").textContent = srVisible;
  document.getElementById("reencode-count").textContent = reencodeVisible;
  document.getElementById("h265-count").textContent = h265Visible;

  const noResults = document.getElementById("no-results");
  noResults.style.display = totalVisible === 0 ? "block" : "none";
}

function toggleSection(header) {
  const chevron = header.querySelector(".chevron");
  const body = header.nextElementSibling;
  const isOpen = body.classList.contains("open");
  body.classList.toggle("open", !isOpen);
  if (chevron) chevron.classList.toggle("open", !isOpen);
}

document.getElementById("search-input").addEventListener("input", e => {
  searchQuery = e.target.value.toLowerCase();
  buildReport();
});

document.querySelectorAll(".filter-btn").forEach(btn => {
  btn.addEventListener("click", () => {
    document.querySelectorAll(".filter-btn").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");
    activeFilter = btn.dataset.filter;
    buildReport();
  });
});

buildReport();
</script>
</body>
</html>
""";

        return beforeData + dataJson + afterData;
    }
}
