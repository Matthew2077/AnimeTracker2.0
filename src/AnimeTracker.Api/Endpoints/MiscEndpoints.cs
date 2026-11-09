using AnimeTracker.Api.Data;
using AnimeTracker.Api.Models;
using AnimeTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Endpoints;

public static class MiscEndpoints
{
    public static void MapMiscEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => new { status = "ok" });

        app.MapGet("/api/genres", async (AppDbContext db) =>
            await db.Genres.OrderBy(g => g.Name).Select(g => new { g.Id, g.Name }).ToListAsync());

        app.MapGet("/api/watching", async (AppDbContext db) =>
        {
            var list = await db.Entries.IncludeAll()
                .Where(e => e.Status == EntryStatus.Ongoing).ToListAsync();
            return list.OrderByDescending(e => e.UpdatedAt).Select(e => e.ToDto()).ToList();
        });

        app.MapGet("/api/stats", async (StatsService stats) => await stats.GetGlobalAsync());
    }
}
