using AnimeTracker.Api.Models;
using AnimeTracker.Api.Services;

namespace AnimeTracker.Api.Dtos;

public record SeriesInput(string Name, int? GenreId, string? CoverFile, string? Notes);

public record SeriesSummaryDto(
    int Id, string Name, int? GenreId, string? GenreName, string? CoverFile, string? Notes,
    DateTime CreatedAt, DateTime UpdatedAt, int EntryCount,
    int AnimeWatched, int AnimeTotal, int OtherWatched, int OtherTotal,
    bool HasManga, bool HasLightNovel, StatsDto Stats);

public record TagDto(int Id, string Name, string? Color);
public record TagInput(string Name, string? Color);

public record EntryInput(
    int SeriesId, string Title, MediaType MediaType, string? Label, UnitLabel UnitLabel,
    int TotalUnits, int WatchedUnits, EntryStatus Status, string? ExternalUrl, string? ImageFile,
    DateTime? CreatedAt, DateTime? CompletedAt, List<string>? Tags);

public record EntryDto(
    int Id, int SeriesId, string SeriesName, string? GenreName, string Title,
    MediaType MediaType, MediaGroup MediaGroup, string? Label, int SortOrder, UnitLabel UnitLabel,
    int TotalUnits, int WatchedUnits, EntryStatus Status, string? ExternalUrl, string? ImageFile,
    DateTime CreatedAt, DateTime? CompletedAt, DateTime UpdatedAt, List<TagDto> Tags);

public record StatusInput(EntryStatus Status);
public record ReorderInput(MediaGroup Group, List<int> OrderedIds);
