namespace AnimeTracker.Api.Models;

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Series> Series { get; set; } = [];
}

public class Series
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? GenreId { get; set; }
    public Genre? Genre { get; set; }
    public string? CoverFile { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Entry> Entries { get; set; } = [];
}

public class Entry
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    public string Title { get; set; } = "";
    public MediaType MediaType { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
    public UnitLabel UnitLabel { get; set; }
    public int TotalUnits { get; set; }
    public int WatchedUnits { get; set; }
    public EntryStatus Status { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ImageFile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Tag> Tags { get; set; } = [];

    // Calcolata, non salvata nel DB
    public MediaGroup MediaGroup => MediaType switch
    {
        MediaType.Manga => MediaGroup.Manga,
        MediaType.LightNovel => MediaGroup.LightNovel,
        _ => MediaGroup.Anime
    };
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public List<Entry> Entries { get; set; } = [];
}