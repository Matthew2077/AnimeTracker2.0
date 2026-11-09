using AnimeTracker.Api.Dtos;
using AnimeTracker.Api.Models;

namespace AnimeTracker.Api.Services;

public static class Validation
{
    public static bool IsSafeFileName(string? f) =>
        string.IsNullOrEmpty(f) || (f == Path.GetFileName(f) && !f.Contains(".."));

    private static bool IsHttpUrl(string? u) =>
        string.IsNullOrEmpty(u) ||
        (Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    public static Dictionary<string, string[]> ValidateSeries(SeriesInput i)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(i.Name)) e["name"] = ["Name is required."];
        if (!IsSafeFileName(i.CoverFile)) e["coverFile"] = ["Invalid file name."];
        return e;
    }

    public static Dictionary<string, string[]> ValidateEntry(EntryInput i)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(i.Title)) e["title"] = ["Title is required."];
        if (!Enum.IsDefined(i.MediaType)) e["mediaType"] = ["Invalid media type."];
        if (!Enum.IsDefined(i.UnitLabel)) e["unitLabel"] = ["Invalid unit label."];
        if (!Enum.IsDefined(i.Status)) e["status"] = ["Invalid status."];
        if (i.TotalUnits <= 0) e["totalUnits"] = ["Total units must be greater than 0."];
        if (i.WatchedUnits < 0 || i.WatchedUnits > i.TotalUnits)
            e["watchedUnits"] = ["Watched units must be between 0 and total units."];
        else if (i.Status == EntryStatus.Completed && i.WatchedUnits != i.TotalUnits)
            e["watchedUnits"] = ["A completed entry must have watched units equal to total units."];
        if (!IsHttpUrl(i.ExternalUrl)) e["externalUrl"] = ["Must be a valid http(s) URL."];
        if (!IsSafeFileName(i.ImageFile)) e["imageFile"] = ["Invalid file name."];
        if (i.Tags?.Any(t => t.Trim().Length > 50) == true) e["tags"] = ["Tag names are max 50 characters."];
        return e;
    }
}
