using AnimeTracker.Api.Data;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Services;

public class OrderingService(AppDbContext db)
{
    /// Riscrive SortOrder da 1 a N per le entry del gruppo, nell'ordine di orderedIds.
    public async Task ReorderAsync(int seriesId, MediaGroup group, IReadOnlyList<int> orderedIds)
    {
        var entries = (await db.Entries.Where(e => e.SeriesId == seriesId).ToListAsync())
            .Where(e => e.MediaGroup == group)
            .ToList();

        if (orderedIds.Count != entries.Count || !entries.Select(e => e.Id).ToHashSet().SetEquals(orderedIds))
            throw new ArgumentException("orderedIds must contain exactly the entries of this series and group.");

        await using var tx = await db.Database.BeginTransactionAsync();
        for (var i = 0; i < orderedIds.Count; i++)
            entries.First(e => e.Id == orderedIds[i]).SortOrder = i + 1;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// SortOrder per una nuova entry: in coda al suo (serie, gruppo).
    public async Task<int> NextSortOrderAsync(int seriesId, MediaType type)
    {
        var group = new Entry { MediaType = type }.MediaGroup;
        var orders = (await db.Entries.Where(e => e.SeriesId == seriesId).ToListAsync())
            .Where(e => e.MediaGroup == group)
            .Select(e => e.SortOrder);
        return orders.DefaultIfEmpty(0).Max() + 1;
    }
}