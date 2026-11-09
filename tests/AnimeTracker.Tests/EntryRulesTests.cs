using AnimeTracker.Api.Models;
using AnimeTracker.Api.Services;

namespace AnimeTracker.Tests;

public class EntryRulesTests
{
    private static Entry MakeEntry(EntryStatus status, int watched, int total = 12) => new()
    {
        Title = "Test", Status = status, WatchedUnits = watched, TotalUnits = total,
        CompletedAt = status == EntryStatus.Completed ? DateTime.UtcNow.AddDays(-1) : null
    };

    [Theory]
    [InlineData(EntryStatus.Planned)]
    [InlineData(EntryStatus.Ongoing)]
    [InlineData(EntryStatus.Dropped)]
    public void AnyToCompleted_SetsWatchedToTotal_AndCompletedAt(EntryStatus from)
    {
        var e = MakeEntry(from, 3);
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        EntryRules.ChangeStatus(e, EntryStatus.Completed, now);
        Assert.Equal(12, e.WatchedUnits);
        Assert.Equal(now, e.CompletedAt);
    }

    [Fact]
    public void OngoingToDropped_KeepsWatched()
    {
        var e = MakeEntry(EntryStatus.Ongoing, 5);
        EntryRules.ChangeStatus(e, EntryStatus.Dropped);
        Assert.Equal(5, e.WatchedUnits);
        Assert.Equal(EntryStatus.Dropped, e.Status);
    }

    [Theory]
    [InlineData(EntryStatus.Ongoing)]
    [InlineData(EntryStatus.Dropped)]
    public void CompletedToOngoingOrDropped_ResetsWatched_AndClearsCompletedAt(EntryStatus to)
    {
        var e = MakeEntry(EntryStatus.Completed, 12);
        EntryRules.ChangeStatus(e, to);
        Assert.Equal(0, e.WatchedUnits);
        Assert.Null(e.CompletedAt);
    }

    [Theory]
    [InlineData(EntryStatus.Ongoing)]
    [InlineData(EntryStatus.Completed)]
    [InlineData(EntryStatus.Dropped)]
    public void AnyToPlanned_ResetsWatched(EntryStatus from)
    {
        var e = MakeEntry(from, from == EntryStatus.Completed ? 12 : 4);
        EntryRules.ChangeStatus(e, EntryStatus.Planned);
        Assert.Equal(0, e.WatchedUnits);
        Assert.Null(e.CompletedAt);
    }

    [Fact]
    public void Increment_ReachingTotal_CompletesEntry()
    {
        var e = MakeEntry(EntryStatus.Ongoing, 11);
        EntryRules.Increment(e);
        Assert.Equal(EntryStatus.Completed, e.Status);
        Assert.Equal(12, e.WatchedUnits);
        Assert.NotNull(e.CompletedAt);
    }

    [Fact]
    public void Increment_BelowTotal_StaysOngoing()
    {
        var e = MakeEntry(EntryStatus.Ongoing, 3);
        EntryRules.Increment(e);
        Assert.Equal(4, e.WatchedUnits);
        Assert.Equal(EntryStatus.Ongoing, e.Status);
    }

    [Fact]
    public void Increment_OnCompleted_Throws()
    {
        var e = MakeEntry(EntryStatus.Completed, 12);
        Assert.Throws<InvalidOperationException>(() => EntryRules.Increment(e));
    }
}