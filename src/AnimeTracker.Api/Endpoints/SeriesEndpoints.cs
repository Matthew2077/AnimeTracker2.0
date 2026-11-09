using AnimeTracker.Api.Data;
using AnimeTracker.Api.Dtos;
using AnimeTracker.Api.Models;
using AnimeTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Endpoints;

public static class SeriesEndpoints
{
    public static void MapSeriesEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/series");

        // GET /api/series?q=&status=&sort=asc|desc
        g.MapGet("/", async (AppDbContext db, string? q, EntryStatus? status, string? sort) =>
        {
            var query = db.Series.Include(s => s.Genre).Include(s => s.Entries).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = $"%{term}%";
                query = query.Where(s => EF.Functions.Like(s.Name, pattern, "\\")
                    || s.Entries.Any(e => EF.Functions.Like(e.Title, pattern, "\\")));
            }

            if (status is { } st)
                query = query.Where(s => s.Entries.Any(e => e.Status == st));

            var list = await query.ToListAsync();
            var ordered = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase)
                ? list.OrderByDescending(s => s.Name, StringComparer.OrdinalIgnoreCase)
                : list.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase);
            return ordered.Select(s => s.ToSummary()).ToList();
        });

        g.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var s = await db.Series.Include(x => x.Genre).Include(x => x.Entries)
                .FirstOrDefaultAsync(x => x.Id == id);
            return s is null ? Results.NotFound() : Results.Ok(s.ToSummary());
        });

        g.MapPost("/", async (SeriesInput input, AppDbContext db) =>
        {
            if (Validation.ValidateSeries(input) is { Count: > 0 } errors)
                return Results.ValidationProblem(errors);

            Genre? genre = null;
            if (input.GenreId is { } gid)
            {
                genre = await db.Genres.FindAsync(gid);
                if (genre is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["genreId"] = ["Unknown genre."] });
            }

            var now = DateTime.UtcNow;
            var s = new Series
            {
                Name = input.Name.Trim(), Genre = genre, GenreId = genre?.Id,
                CoverFile = input.CoverFile, Notes = input.Notes, CreatedAt = now, UpdatedAt = now
            };
            db.Series.Add(s);
            await db.SaveChangesAsync();
            return Results.Created($"/api/series/{s.Id}", s.ToSummary());
        });

        g.MapPut("/{id:int}", async (int id, SeriesInput input, AppDbContext db) =>
        {
            if (Validation.ValidateSeries(input) is { Count: > 0 } errors)
                return Results.ValidationProblem(errors);

            var s = await db.Series.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();

            Genre? genre = null;
            if (input.GenreId is { } gid)
            {
                genre = await db.Genres.FindAsync(gid);
                if (genre is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["genreId"] = ["Unknown genre."] });
            }

            s.Name = input.Name.Trim();
            s.Genre = genre;
            s.GenreId = genre?.Id;
            s.CoverFile = input.CoverFile;
            s.Notes = input.Notes;
            s.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(s.ToSummary());
        });

        // Elimina anche le entry (cascade nel DB)
        g.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var n = await db.Series.Where(s => s.Id == id).ExecuteDeleteAsync();
            return n == 0 ? Results.NotFound() : Results.NoContent();
        });

        g.MapGet("/{id:int}/entries", async (int id, AppDbContext db) =>
        {
            if (!await db.Series.AnyAsync(s => s.Id == id)) return Results.NotFound();
            var list = await db.Entries.IncludeAll().Where(e => e.SeriesId == id).ToListAsync();
            return Results.Ok(list.OrderBy(e => e.MediaGroup).ThenBy(e => e.SortOrder)
                .Select(e => e.ToDto()).ToList());
        });

        g.MapPost("/{id:int}/reorder", async (int id, ReorderInput input, AppDbContext db, OrderingService ordering) =>
        {
            if (!await db.Series.AnyAsync(s => s.Id == id)) return Results.NotFound();
            try
            {
                await ordering.ReorderAsync(id, input.Group, input.OrderedIds);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}