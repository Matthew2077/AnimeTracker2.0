using AnimeTracker.Api.Data;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Services;

public record StatsDto(int Completed, int Ongoing, int Dropped, int Planned);

public class StatsService(AppDbContext db)
{
    public async Task<StatsDto> GetGlobalAsync()
    {
        var counts = await db.Entries
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Get(EntryStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;
        return new StatsDto(Get(EntryStatus.Completed), Get(EntryStatus.Ongoing),
                            Get(EntryStatus.Dropped), Get(EntryStatus.Planned));
    }
}