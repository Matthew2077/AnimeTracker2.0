using AnimeTracker.Api.Data;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Services;

public static class TagHelper
{
    /// Trova i tag per nome (senza distinzione maiuscole) e crea quelli mancanti.
    public static async Task<List<Tag>> ResolveAsync(AppDbContext db, IEnumerable<string>? names)
    {
        var wanted = (names ?? [])
            .Select(n => n.Trim()).Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (wanted.Count == 0) return [];

        var existing = await db.Tags.ToListAsync();
        var result = new List<Tag>();
        foreach (var name in wanted)
        {
            var tag = existing.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                tag = new Tag { Name = name };
                db.Tags.Add(tag);
                existing.Add(tag);
            }
            result.Add(tag);
        }
        return result;
    }
}
