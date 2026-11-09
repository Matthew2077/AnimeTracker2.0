using AnimeTracker.Api.Models;

namespace AnimeTracker.Api.Services;

public static class EntryRules
{
    public static void ChangeStatus(Entry entry, EntryStatus newStatus, DateTime? now = null)
    {
        var ts = now ?? DateTime.UtcNow;
        var old = entry.Status;

        switch (newStatus)
        {
            case EntryStatus.Completed:
                entry.WatchedUnits = entry.TotalUnits;
                entry.CompletedAt = ts;
                break;

            case EntryStatus.Planned:
                entry.WatchedUnits = 0;
                entry.CompletedAt = null;
                break;

            case EntryStatus.Ongoing or EntryStatus.Dropped when old == EntryStatus.Completed:
                entry.WatchedUnits = 0;
                entry.CompletedAt = null;
                break;

            // ongoing -> dropped (e qualsiasi altro caso): visti invariati
        }

        entry.Status = newStatus;
        entry.UpdatedAt = ts;
    }

    public static void Increment(Entry entry, DateTime? now = null)
    {
        if (entry.Status == EntryStatus.Completed || entry.WatchedUnits >= entry.TotalUnits)
            throw new InvalidOperationException("Entry is already completed.");

        entry.WatchedUnits++;

        if (entry.WatchedUnits >= entry.TotalUnits)
            ChangeStatus(entry, EntryStatus.Completed, now);
        else
        {
            if (entry.Status != EntryStatus.Ongoing)
                entry.Status = EntryStatus.Ongoing;
            entry.UpdatedAt = now ?? DateTime.UtcNow;
        }
    }
}