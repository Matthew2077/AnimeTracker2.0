using AnimeTracker.Api.Data;
using AnimeTracker.Api.Dtos;
using AnimeTracker.Api.Models;
using AnimeTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Endpoints;

public static class EntryEndpoints
{
    private static void Apply(Entry e, EntryInput i)
    {
        e.Title = i.Title.Trim();
        e.MediaType = i.MediaType;
        e.Label = string.IsNullOrWhiteSpace(i.Label) ? null : i.Label.Trim();
        e.UnitLabel = i.UnitLabel;
        e.TotalUnits = i.TotalUnits;
        e.WatchedUnits = i.WatchedUnits;
        e.Status = i.Status;
        e.ExternalUrl = string.IsNullOrWhiteSpace(i.ExternalUrl) ? null : i.ExternalUrl.Trim();
        e.ImageFile = string.IsNullOrWhiteSpace(i.ImageFile) ? null : i.ImageFile;
    }

    private static IResult SeriesNotFound() =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["seriesId"] = ["Series not found."] });

    public static void MapEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/entries");

        g.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var e = await db.Entries.IncludeAll().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? Results.NotFound() : Results.Ok(e.ToDto());
        });

        g.MapPost("/", async (EntryInput input, AppDbContext db, OrderingService ordering) =>
        {
            if (Validation.ValidateEntry(input) is { Count: > 0 } errors)
                return Results.ValidationProblem(errors);

            var series = await db.Series.Include(s => s.Genre).FirstOrDefaultAsync(s => s.Id == input.SeriesId);
            if (series is null) return SeriesNotFound();

            var now = DateTime.UtcNow;
            var entry = new Entry
            {
                Series = series, SeriesId = series.Id,
                CreatedAt = input.CreatedAt ?? now, UpdatedAt = now
            };
            Apply(entry, input);
            entry.CompletedAt = input.Status == EntryStatus.Completed ? input.CompletedAt ?? now : null;
            entry.SortOrder = await ordering.NextSortOrderAsync(series.Id, input.MediaType);
            entry.Tags = await TagHelper.ResolveAsync(db, input.Tags);

            db.Entries.Add(entry);
            await db.SaveChangesAsync();
            return Results.Created($"/api/entries/{entry.Id}", entry.ToDto());
        });

        g.MapPut("/{id:int}", async (int id, EntryInput input, AppDbContext db, OrderingService ordering) =>
        {
            if (Validation.ValidateEntry(input) is { Count: > 0 } errors)
                return Results.ValidationProblem(errors);

            var entry = await db.Entries.IncludeAll().FirstOrDefaultAsync(e => e.Id == id);
            if (entry is null) return Results.NotFound();

            var series = entry.Series;
            if (input.SeriesId != entry.SeriesId)
            {
                series = await db.Series.Include(s => s.Genre).FirstOrDefaultAsync(s => s.Id == input.SeriesId);
                if (series is null) return SeriesNotFound();
            }

            // Se cambia serie o gruppo, l'entry va in coda al nuovo (serie, gruppo)
            var newGroup = new Entry { MediaType = input.MediaType }.MediaGroup;
            if (series.Id != entry.SeriesId || newGroup != entry.MediaGroup)
                entry.SortOrder = await ordering.NextSortOrderAsync(series.Id, input.MediaType);

            var now = DateTime.UtcNow;
            Apply(entry, input);
            entry.Series = series;
            entry.SeriesId = series.Id;
            if (input.CreatedAt is { } created) entry.CreatedAt = created;
            entry.CompletedAt = input.Status == EntryStatus.Completed
                ? input.CompletedAt ?? entry.CompletedAt ?? now
                : null;
            entry.UpdatedAt = now;

            var tags = await TagHelper.ResolveAsync(db, input.Tags);
            entry.Tags.Clear();
            entry.Tags.AddRange(tags);

            await db.SaveChangesAsync();
            return Results.Ok(entry.ToDto());
        });

        g.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var n = await db.Entries.Where(e => e.Id == id).ExecuteDeleteAsync();
            return n == 0 ? Results.NotFound() : Results.NoContent();
        });

        g.MapPost("/{id:int}/status", async (int id, StatusInput input, AppDbContext db) =>
        {
            if (!Enum.IsDefined(input.Status))
                return Results.BadRequest(new { error = "Invalid status." });
            var entry = await db.Entries.IncludeAll().FirstOrDefaultAsync(e => e.Id == id);
            if (entry is null) return Results.NotFound();

            EntryRules.ChangeStatus(entry, input.Status);
            await db.SaveChangesAsync();
            return Results.Ok(entry.ToDto());
        });

        g.MapPost("/{id:int}/increment", async (int id, AppDbContext db) =>
        {
            var entry = await db.Entries.IncludeAll().FirstOrDefaultAsync(e => e.Id == id);
            if (entry is null) return Results.NotFound();
            try
            {
                EntryRules.Increment(entry);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            await db.SaveChangesAsync();
            return Results.Ok(entry.ToDto());
        });
    }
}