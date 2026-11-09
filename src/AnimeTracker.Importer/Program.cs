using System.Text;
using System.Text.Json;
using AnimeTracker.Api.Data;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

// Uso (dalla root della soluzione, con l'API ferma):
//   dotnet run --project src/AnimeTracker.Importer -- <data.json> <cartella-immagini> [--dry-run]
var positional = args.Where(a => !a.StartsWith("--")).ToList();
var dryRun = args.Contains("--dry-run");
if (positional.Count < 2)
{
    Console.WriteLine("Usage: dotnet run --project src/AnimeTracker.Importer -- <data.json> <images-dir> [--dry-run]");
    return 1;
}

var jsonPath = Path.GetFullPath(positional[0]);
var imagesDir = Path.GetFullPath(positional[1]);
if (!File.Exists(jsonPath)) { Console.WriteLine($"JSON file not found: {jsonPath}"); return 1; }
if (!Directory.Exists(imagesDir)) { Console.WriteLine($"Images folder not found: {imagesDir}"); return 1; }

// --- Lettura JSON (accetta un array, oppure un oggetto che contiene un array) ---
List<OldAnime> old;
try
{
    using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
    JsonElement arr = default;
    if (doc.RootElement.ValueKind == JsonValueKind.Array)
        arr = doc.RootElement;
    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        foreach (var p in doc.RootElement.EnumerateObject())
            if (p.Value.ValueKind == JsonValueKind.Array) { arr = p.Value; break; }

    if (arr.ValueKind != JsonValueKind.Array)
    {
        Console.WriteLine("Could not find a list of anime in the JSON file.");
        return 1;
    }
    old = arr.Deserialize<List<OldAnime>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
    return 1;
}

// --- Database (stesso percorso dell'API: <root>/data/tracker.db) ---
var root = Directory.GetCurrentDirectory();
var dataDir = Path.Combine(root, "data");
var uploadsDir = Path.Combine(root, "uploads");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(uploadsDir);

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={Path.Combine(dataDir, "tracker.db")}").Options;
await using var db = new AppDbContext(options);
await db.Database.MigrateAsync();

if (await db.Series.AnyAsync())
{
    Console.WriteLine("The database already contains series. Delete data/tracker.db and uploads/ to import again from scratch.");
    return 1;
}

var genres = (await db.Genres.ToListAsync()).ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
var tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
var filesToCopy = new HashSet<string>();
var skipped = new List<string>();
var warnings = new List<string>();
int importedAnime = 0, importedManga = 0;
var now = DateTime.UtcNow;

await using var tx = await db.Database.BeginTransactionAsync();

for (var i = 0; i < old.Count; i++)
{
    var o = old[i];
    var who = $"#{i + 1} \"{o.Title}\"";
    var problems = new List<string>();

    var title = o.Title?.Trim() ?? "";
    if (title.Length == 0) problems.Add("missing title");

    EntryStatus status = default;
    if (!(Enum.TryParse(o.Status, true, out status) && Enum.IsDefined(status)))
        problems.Add($"unknown status \"{o.Status}\"");

    if (o.TotalEpisodes is not > 0)
        problems.Add($"invalid total units ({o.TotalEpisodes?.ToString() ?? "null"})");
    if (o.EpisodesWatched is null or < 0)
        problems.Add($"invalid watched units ({o.EpisodesWatched?.ToString() ?? "null"})");
    else if (o.TotalEpisodes is > 0 && o.EpisodesWatched > o.TotalEpisodes)
        problems.Add($"watched ({o.EpisodesWatched}) is greater than total ({o.TotalEpisodes})");
    else if (o.TotalEpisodes is > 0 && status == EntryStatus.Completed && o.EpisodesWatched != o.TotalEpisodes)
        problems.Add($"status is completed but watched is {o.EpisodesWatched}/{o.TotalEpisodes}");

    if (problems.Count > 0)
    {
        skipped.Add($"{who}: {string.Join("; ", problems)}");
        continue;
    }

    var total = o.TotalEpisodes!.Value;
    var watched = o.EpisodesWatched!.Value;

    // Tipo: season "Manga" -> manga, altrimenti anime con Label = season
    var isManga = string.Equals(o.Season?.Trim(), "manga", StringComparison.OrdinalIgnoreCase);
    var mediaType = isManga ? MediaType.Manga : MediaType.Anime;
    var unit = isManga ? UnitLabel.Chapters : UnitLabel.Episodes;
    var label = isManga || string.IsNullOrWhiteSpace(o.Season) ? null : o.Season.Trim();

    // Generi: il primo (se nella lista) alla serie, gli altri diventano tag dell'entry
    var genreNames = (o.Genres ?? [])
        .Select(g => g.Trim()).Where(g => g.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    Genre? genre = null;
    if (genreNames.Count > 0)
    {
        if (genres.TryGetValue(genreNames[0], out var g0)) genre = g0;
        else warnings.Add($"{who}: first genre \"{genreNames[0]}\" is not in the genre list, series genre left empty");
    }
    var entryTags = new List<Tag>();
    foreach (var name in genreNames.Skip(1))
    {
        if (!tags.TryGetValue(name, out var tag))
        {
            tag = new Tag { Name = name };
            tags[name] = tag;
        }
        entryTags.Add(tag);
    }

    // Immagine
    string? image = null;
    if (!string.IsNullOrWhiteSpace(o.ImageFilename))
    {
        var f = o.ImageFilename.Trim();
        if (f != Path.GetFileName(f))
            warnings.Add($"{who}: invalid image file name \"{f}\", imported without image");
        else if (!File.Exists(Path.Combine(imagesDir, f)))
            warnings.Add($"{who}: image file \"{f}\" not found, imported without image");
        else
        {
            image = f;
            filesToCopy.Add(f);
        }
    }

    // Link: il primo http(s) valido, gli altri vanno nel report
    var allLinks = o.Links ?? [];
    var validUrls = allLinks
        .Select(l => l.Url?.Trim())
        .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .ToList();
    var url = validUrls.FirstOrDefault();
    if (validUrls.Count > 1)
        warnings.Add($"{who}: {validUrls.Count - 1} extra link(s) not imported: {string.Join(", ", validUrls.Skip(1))}");
    if (allLinks.Count > validUrls.Count)
        warnings.Add($"{who}: {allLinks.Count - validUrls.Count} link(s) with an invalid URL ignored");

    // Date
    if (o.DateAdded is null) warnings.Add($"{who}: missing dateAdded, import date used");
    var added = (o.DateAdded ?? now).ToUniversalTime();
    DateTime? completedAt = status == EntryStatus.Completed ? (o.DateCompleted ?? now).ToUniversalTime() : null;

    var series = new Series
    {
        Name = title, Genre = genre, CoverFile = image, CreatedAt = added, UpdatedAt = added
    };
    series.Entries.Add(new Entry
    {
        Title = title, MediaType = mediaType, Label = label, SortOrder = 1, UnitLabel = unit,
        TotalUnits = total, WatchedUnits = watched, Status = status, ExternalUrl = url,
        ImageFile = image, CreatedAt = added, CompletedAt = completedAt, UpdatedAt = added,
        Tags = entryTags
    });
    db.Series.Add(series);

    if (isManga) importedManga++; else importedAnime++;
}

// Il SaveChanges applica anche i check constraint del DB: se qualcosa sfugge, fallisce e fa rollback
await db.SaveChangesAsync();

if (!dryRun)
{
    foreach (var f in filesToCopy)
    {
        var dst = Path.Combine(uploadsDir, f);
        if (!File.Exists(dst)) File.Copy(Path.Combine(imagesDir, f), dst);
    }
    await tx.CommitAsync();
}
else
{
    await tx.RollbackAsync();
}

// --- Report ---
var sb = new StringBuilder();
sb.AppendLine(dryRun ? "IMPORT REPORT (DRY RUN: nothing was written)" : "IMPORT REPORT");
sb.AppendLine($"Read: {old.Count} | Imported: {importedAnime + importedManga} ({importedAnime} anime, {importedManga} manga) | Skipped: {skipped.Count} | Images: {filesToCopy.Count}");
sb.AppendLine();
sb.AppendLine($"SKIPPED ({skipped.Count})");
foreach (var s in skipped) sb.AppendLine($"  - {s}");
sb.AppendLine();
sb.AppendLine($"WARNINGS ({warnings.Count})");
foreach (var w in warnings) sb.AppendLine($"  - {w}");

var report = sb.ToString();
Console.WriteLine(report);
File.WriteAllText(Path.Combine(root, "import-report.txt"), report);
return 0;

// --- Formato v1.1 ---
record OldLink(string? Name, string? Url);

record OldAnime(
    string? Title, string? Season, int? EpisodesWatched, int? TotalEpisodes, string? Status,
    DateTime? DateAdded, DateTime? DateCompleted, List<string>? Genres, string? ImageFilename,
    List<OldLink>? Links);
