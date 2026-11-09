using AnimeTracker.Api.Dtos;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Services;

public static class Mapping
{
    public static IQueryable<Entry> IncludeAll(this IQueryable<Entry> q) =>
        q.Include(e => e.Series).ThenInclude(s => s.Genre).Include(e => e.Tags);

    public static StatsDto CountByStatus(IEnumerable<Entry> entries)
    {
        var list = entries.ToList();
        return new StatsDto(
            list.Count(e => e.Status == EntryStatus.Completed),
            list.Count(e => e.Status == EntryStatus.Ongoing),
            list.Count(e => e.Status == EntryStatus.Dropped),
            list.Count(e => e.Status == EntryStatus.Planned));
    }

    public static SeriesSummaryDto ToSummary(this Series s)
    {
        var anime = s.Entries.Where(e => e.MediaGroup == MediaGroup.Anime).ToList();
        var other = s.Entries.Where(e => e.MediaGroup != MediaGroup.Anime).ToList();
        return new SeriesSummaryDto(
            s.Id, s.Name, s.GenreId, s.Genre?.Name, s.CoverFile, s.Notes,
            s.CreatedAt, s.UpdatedAt, s.Entries.Count,
            anime.Sum(e => e.WatchedUnits), anime.Sum(e => e.TotalUnits),
            other.Sum(e => e.WatchedUnits), other.Sum(e => e.TotalUnits),
            s.Entries.Any(e => e.MediaGroup == MediaGroup.Manga),
            s.Entries.Any(e => e.MediaGroup == MediaGroup.LightNovel),
            CountByStatus(s.Entries));
    }

    public static EntryDto ToDto(this Entry e) => new(
        e.Id, e.SeriesId, e.Series.Name, e.Series.Genre?.Name, e.Title,
        e.MediaType, e.MediaGroup, e.Label, e.SortOrder, e.UnitLabel,
        e.TotalUnits, e.WatchedUnits, e.Status, e.ExternalUrl, e.ImageFile,
        e.CreatedAt, e.CompletedAt, e.UpdatedAt,
        e.Tags.OrderBy(t => t.Name).Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList());
}
