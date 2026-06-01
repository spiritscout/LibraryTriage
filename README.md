# LibraryTriage

A standalone media library analysis tool that scans a Jellyfin library and categorises each file by what intervention it actually needs. Rather than blindly processing everything, it produces per-file recommendations so you can make informed decisions about re-encoding, super-resolution upscaling, or leaving files alone.

Built in C# as part of a broader set of projects centred around a large personal Jellyfin library (~1500 films, several hundred TV shows and short films).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [FFmpeg](https://ffmpeg.org/download.html) (includes FFprobe) — install via `winget install ffmpeg` on Windows

Both must be available on your system PATH.

---

## Setup

```bash
git clone https://github.com/spiritscout/LibraryTriage.git
cd LibraryTriage
dotnet build LibraryTriage.sln
```

---

## Configuration

Tunable thresholds and settings live in `src/LibraryTriage.CLI/appsettings.json`. The file is copied to the build output and read at runtime.

```json
{
    "Classification": {
        "YearThresholdLower": 2000,
        "YearThresholdUpper": 2008,
        "MinHeightForSR": 720,
        "H264BitrateDensityThresholdLower": 0.03,
        "H264BitrateDensityThresholdHigher": 0.1,
        "H265BitrateDensityThreshold": 0.06
    },
    "Discovery": {
        "VideoExtensions": [".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".mpg", ".mpeg", ".ts", ".m2ts", ".webm"]
    },
    "Output": {
        "AutoOpenReport": false
    }
}
```

- **Classification** — thresholds used by the classifier. See "Classification Rules" below for what each value controls.
- **Discovery** — file extensions to scan for. Anything not in this list is silently skipped.
- **Output** — `AutoOpenReport` opens the HTML report in your default browser when the scan completes.

Edit and save the file, rebuild with `dotnet build`, and your new values take effect on the next run.

## How to Run

Point the tool at your library root — the folder containing your Movies, Shows, and/or Shorts directories:

```bash
dotnet run --project src/LibraryTriage.CLI "C:\Path\To\Your\Library"
```

The tool will scan all video files recursively, process each one through FFprobe, classify it, and write a report. Progress is printed to the console as files are processed.

When complete, a `triage_report.html` file is written to the library root. Open it in any browser.

### Expected folder structure

The tool expects your library to follow this naming convention:

```
Library Root/
├── Movies/          (or Movies 2, Movies 3 etc)
├── Shows/           (or Shows 2, Shows 3 etc)
└── Shorts/
```

TV show folders should follow the convention `Show Name (Year)` with season subfolders, e.g:

```
Shows/
└── ER (1994)/
    ├── Season 01/
    │   └── ER.S01E01.mkv
    └── Season 02/
```

Film and short folders should include the year in parentheses in the path, e.g. `The Godfather (1972)`.

---

## Project Structure

```
LibraryTriage/
├── src/
│   ├── LibraryTriage.Core/          # All analysis logic
│   │   ├── FFprobe/
│   │   │   ├── FFprobeOutput.cs     # Typed model of FFprobe's JSON output
│   │   │   └── FFprobeRunner.cs     # Spawns FFprobe as a subprocess
│   │   ├── Models/
│   │   │   ├── MediaFile.cs         # Internal file representation with parsed metadata
│   │   │   └── ClassificationResult.cs  # Result model including enums
│   │   ├── Analysis/
│   │   │   └── Classifier.cs        # Classification rules and scoring logic
│   │   ├── Discovery/
│   │   │   └── FileDiscovery.cs     # Recursive file scanning with extension filtering
│   │   └── Output/
│   │       └── ReportWriter.cs      # HTML report generation
│   └── LibraryTriage.CLI/
│       └── Program.cs               # Entry point, wires everything together
```

---

## Classification Rules

Each file is evaluated against a set of signals and assigned one or more recommendations. Files can receive multiple recommendations simultaneously.

### Recommendations

| Recommendation | Meaning |
|---|---|
| `SRCandidate` | File would benefit from super-resolution upscaling |
| `ReencodeRecommended` | Current encode is inefficient, re-encoding would reduce file size |
| `H265UpgradeRecommended` | H264 file where H265 re-encoding would give significant storage savings |
| `AlreadyH265` | Informational — file is already H265, no action implied |
| `LeaveAlone` | No intervention needed |

`LeaveAlone` and `AlreadyH265`-only files are excluded from the HTML report. Only actionable files appear.

### SR Candidate Scoring

SR candidacy is scored rather than binary, producing a confidence level alongside the recommendation.

| Signal | Points |
|---|---|
| Old codec (mpeg2video, msmpeg4v3/DivX, xvid, wmv2, wmv3, vc1) | 4 |
| Height below 720px | 4 |
| H264 with bitrate density below 0.03 | 3 |
| Production year pre-2000 (effective) | 2 |
| Production year 2000–2008 (effective) | 1 |

**Confidence thresholds:**
- High — 8+ points
- Medium — 4–7 points
- Low — 1–3 points

**Effective year calculation**

For TV shows, the production year is approximated per-season by adding the season number to the show's premiere year — e.g. a show premiering in 2003 has an effective year of 2008 by Season 5. This avoids flagging later seasons of long-running shows that aired well into the HD era. Season 0 and Season 1 are evaluated on the premiere year directly.

The two year thresholds reflect two meaningful industry transitions:
- **2000** — the year ER made its first HD move (Season 7, 1080i), marking the beginning of the HD transition for US broadcast drama. ER was a production trendsetter and this is a widely cited early adoption point.
- **2008** — by which point HD had become standard practice for new US broadcast productions.

Content in the transitional 2000–2008 window scores 1 point rather than 2, reflecting genuine uncertainty — production quality in this era varied considerably depending on the show, the network, and whether the editing pipeline matched the capture format.

The encoder tag (HandBrake, Lavf, etc.) is included in the reasoning string as advisory context but does not affect scoring.

### Re-encode Thresholds

| Codec | Bitrate density threshold |
|---|---|
| H264 | above 0.1 |
| H265 | above 0.06 |

Bitrate density is calculated as: `bitrate / (width × height × framerate)` — bits per pixel per second. This accounts for resolution when assessing encoding efficiency, unlike raw bitrate or file size alone.

> **Note:** These thresholds are starting points and will need tuning against your specific library. Animation compresses much more efficiently than live action at equivalent quality, so animated content may produce false positives. This is a known limitation — see below.

### H265 Upgrade

Applied to H264 files with bitrate density above 0.1. Advisory only — whether to act on it depends on your playback setup. H265 requires hardware decoding support; if your playback device lacks this, the server must transcode, which adds CPU load.

---

## Output

The HTML report groups results by category (Movies, Shows, Shorts) with collapsible sections. TV shows are organised by show name and then by season.

Each file entry shows:
- Cleaned episode name where possible (e.g. `S03E10 — Tokyo Colony` parsed from messy release filenames), falling back to the raw filename otherwise
- Codec, resolution, file size, MB/min
- Colour-coded recommendation badges
- Confidence level (SR candidates only)
- Reasoning string explaining which signals fired

**Badge colours:**
- SR Candidate High — red
- SR Candidate Medium — amber
- SR Candidate Low — yellow
- Re-encode Recommended — blue
- H265 Upgrade Recommended — purple

The report includes a search bar and filter buttons for navigating large results.

---

## Known Limitations

**Animation vs live action** — bitrate density thresholds cannot distinguish animated content from live action. Animation compresses far more efficiently, so animated files may be incorrectly flagged as re-encode candidates. Frame-level analysis would be needed to address this properly.

**Long-running TV shows** — the production year is parsed from the show folder name (e.g. `NCIS (2003)`), which means every episode of a long-running show gets flagged with the premiere year, including later seasons that aired well after the 2008 cutoff. Resolving this properly would require per-episode air date lookup from an external source like TVDB.

**Episode filename cleaning is heuristic** — the tool extracts the `SxxExx` marker and episode title from messy release filenames using regex pattern matching against a list of known quality/codec tags. This handles common scene release naming conventions well but isn't exhaustive — unusual release formats may not clean correctly, and dots in actual episode titles (e.g. `M.I.A.`) get converted to spaces during cleaning.

**Metadata-only analysis** — the current version uses FFprobe metadata only. Bitrate, codec, and resolution are reliable signals but cannot detect source quality degradation, compression artefacts, or the difference between a clean encode and a layered DVD-rip. Frame analysis (planned) would address these cases.

**H265 files as SR candidates** — content already re-encoded to H265 from a poor quality source will score lower on SR signals than it deserves, since the old codec and bitrate density signals don't fire for H265. The year and resolution signals still contribute, giving medium confidence for content like pre-2008 TV shows re-encoded to H265 at 720p.

---

## Roadmap

**Higher priority:**
- Docker containerisation for running against a library on a different machine
- Per-episode air date lookup to better handle long-running shows

**Medium priority:**
- Improved episode filename cleaning for edge cases
- GUI application (WPF/MAUI) wrapping the existing core logic

**Lower priority:**
- Frame analysis layer — computational artefact and noise detection without ML, to extend beyond metadata-only analysis
- Animation detection — distinguishing animated from live action content for more accurate bitrate density thresholds

---

## Related Projects

LibraryTriage is one of four connected projects built around the same Jellyfin library:

**Project 2 — Adaptive SR Pipeline** — video super-resolution built on BasicVSR with content-adaptive routing. LibraryTriage's SR candidate list and degradation metadata are designed to feed directly into this pipeline's input.

**Project 3 — Enhancement Layer Tool** — experimental tool for storing compact enhancement layers alongside original files rather than replacing them, combining on demand at playback. LCEVC is a candidate standard.

**Project 1 — X-Ray Emulator** — Jellyfin plugin replicating Amazon Prime's X-Ray feature using local face recognition against a TMDb-sourced celebrity database.
